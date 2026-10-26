using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace Backend.Services.TechProcess;

public class ParsedTechProcess
{
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public List<ParsedTechProcessOperation> Operations { get; set; } = new();
}

public class ParsedTechProcessOperation
{
    public int? OpNo { get; set; }
    public string? OpType { get; set; }
    public string? BlankCode { get; set; }
    public double? SetupTimeMin { get; set; }
    public double? ProcessTimeMin { get; set; }
    public double? ServiceTimeMin { get; set; }
    public string? EquipmentCode { get; set; }
    public List<string> ToolCodes { get; set; } = new();
    public string? ProfessionCode { get; set; }
    public string? Description { get; set; }
    public string? Extra { get; set; }
}

public static class TechProcessXlsxParser
{
    // Заголовки из вашего XLSX
    private const string H_OpNo = "Номер операции";
    private const string H_OpType = "Тип операции";
    private const string H_Blank = "Заготовка";
    private const string H_Setup = "Время наладки оборудования для операции, мин";
    private const string H_Process = "Время операции, мин";
    private const string H_Service = "Время обслуживания оборудования после операции, мин";
    private const string H_Equipment = "Код необходимого оборудования";
    private const string H_Tools = "Коды необходимых инструментов";
    private const string H_Profession = "Код необходимой профессии";
    private const string H_Desc = "Описание операции";
    private const string H_Extra = "Дополнительные характеристики";

    public static ParsedTechProcess ParseSingleSheet(Stream xlsxStream, string sheetName)
    {
        // Required for ExcelDataReader on .NET
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        var table = dataSet.Tables.Cast<DataTable>().FirstOrDefault(t => t.TableName == sheetName);
        if (table == null)
            throw new InvalidOperationException($"Sheet '{sheetName}' not found in XLSX");

        if (table.Rows.Count == 0)
            throw new InvalidOperationException($"Sheet '{sheetName}' is empty");

        // Find header row (we expect it at row 0, but keep robust)
        int headerRowIndex = FindHeaderRowIndex(table);
        if (headerRowIndex < 0)
            throw new InvalidOperationException($"Header row not found in sheet '{sheetName}'");

        var headerMap = BuildHeaderIndexMap(table.Rows[headerRowIndex]);

        var parsed = new ParsedTechProcess
        {
            SheetName = sheetName,
            Code = ExtractTpCode(sheetName),
            Name = ExtractTpName(sheetName)
        };

        for (int r = headerRowIndex + 1; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];

            // stop if row is totally empty
            if (IsRowEmpty(row))
                continue;

            // require at least opNo or description to consider it an operation
            var opNo = TryParseInt(GetCell(row, headerMap, H_OpNo));
            var desc = GetCell(row, headerMap, H_Desc);
            if (opNo == null && string.IsNullOrWhiteSpace(desc))
                continue;

            parsed.Operations.Add(new ParsedTechProcessOperation
            {
                OpNo = opNo,
                OpType = NullIfEmpty(GetCell(row, headerMap, H_OpType)),
                BlankCode = NullIfEmpty(GetCell(row, headerMap, H_Blank)),
                SetupTimeMin = TryParseDouble(GetCell(row, headerMap, H_Setup)),
                ProcessTimeMin = TryParseDouble(GetCell(row, headerMap, H_Process)),
                ServiceTimeMin = TryParseDouble(GetCell(row, headerMap, H_Service)),
                EquipmentCode = NullIfEmpty(GetCell(row, headerMap, H_Equipment)),
                ToolCodes = SplitCodes(GetCell(row, headerMap, H_Tools)),
                ProfessionCode = NullIfEmpty(GetCell(row, headerMap, H_Profession)),
                Description = NullIfEmpty(GetCell(row, headerMap, H_Desc)),
                Extra = NullIfEmpty(GetCell(row, headerMap, H_Extra))
            });
        }

        return parsed;
    }

    private static int FindHeaderRowIndex(DataTable table)
    {
        // Try first 10 rows
        var max = Math.Min(10, table.Rows.Count);
        for (int i = 0; i < max; i++)
        {
            var row = table.Rows[i];
            var values = row.ItemArray.Select(v => v?.ToString()?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (values.Count == 0) continue;

            if (values.Contains(H_OpNo) && values.Contains(H_OpType) && values.Contains(H_Desc))
                return i;
        }
        return -1;
    }

    private static Dictionary<string, int> BuildHeaderIndexMap(DataRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < headerRow.ItemArray.Length; c++)
        {
            var key = headerRow[c]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (!map.ContainsKey(key))
                map[key] = c;
        }
        return map;
    }

    private static string GetCell(DataRow row, Dictionary<string, int> headerMap, string header)
    {
        if (!headerMap.TryGetValue(header, out var idx))
            return string.Empty;
        if (idx < 0 || idx >= row.ItemArray.Length)
            return string.Empty;
        return row[idx]?.ToString()?.Trim() ?? string.Empty;
    }

    private static bool IsRowEmpty(DataRow row)
    {
        foreach (var v in row.ItemArray)
        {
            if (v == null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return false;
        }
        return true;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static int? TryParseInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            return v;
        // sometimes excel gives "10.0"
        if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var dv))
            return (int)dv;
        return null;
    }

    private static double? TryParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    private static List<string> SplitCodes(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<string>();
        return Regex.Split(s, "[;,\r\n\t ]+")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ExtractTpCode(string sheetName)
    {
        var m = Regex.Match(sheetName, @"^(ТП-\d+)");
        return m.Success ? m.Groups[1].Value : sheetName;
    }

    private static string? ExtractTpName(string sheetName)
    {
        // tries «name»
        var m = Regex.Match(sheetName, @"«(?<n>[^»]+)»");
        if (m.Success) return m.Groups["n"].Value.Trim();

        // fallback after code
        var code = ExtractTpCode(sheetName);
        var rest = sheetName.Replace(code, string.Empty).Trim(' ', '-', '–', '—');
        return string.IsNullOrWhiteSpace(rest) ? null : rest;
    }
}
