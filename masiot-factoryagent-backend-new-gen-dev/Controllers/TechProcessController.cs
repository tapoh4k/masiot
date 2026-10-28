using System.Data;
using System.Text.Json;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Models.Messages;
using Backend.Services.Agents;
using Backend.Services.RabbitMQ;
using Backend.Services.TechProcess;
using static Backend.Services.TechProcess.TechProcessXlsxParser;

namespace Backend.Controllers;

[ApiController]
[Route("api/tech-process")]
public class TechProcessController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IAgentLifecycleService _lifecycle;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TechProcessController> _logger;

    public TechProcessController(
        ApplicationDbContext context,
        IRabbitMQService rabbitMQService,
        IAgentLifecycleService lifecycle,
        IConfiguration configuration,
        ILogger<TechProcessController> logger)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _lifecycle = lifecycle;
        _configuration = configuration;
        _logger = logger;
    }

    public class AttachTechProcessRequest
    {
        public int AgentId { get; set; }
        public string SheetName { get; set; } = string.Empty;
    }

    public const string TpFolderName = "Технологические процессы";
    private const string LegacyTpFolderName = "ТП";
    private const string DefaultTpWorkflow = "TECH_PROCESS_DEFAULT_WORKFLOW";

    // POST api/tech-process/attach (multipart: file)
    // Все листы ТП-* попадают в одну папку "Технологические процессы": на каждый лист
    // создается TechProcess со всеми операциями и агент типа tech_process.
    // Уже импортированные ТП (тот же код и обозначение) пропускаются.
    [HttpPost("attach")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<object>> Attach([FromForm] IFormFile file, [FromForm] string? sheetName = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "XLSX file is required" });

            var allSheets = ParseAllSheetsFromXlsx(file);
            var tpSheets = allSheets
                .Where(s => IsTpSheet(s.SheetName))
                .Where(s => string.IsNullOrWhiteSpace(sheetName) || s.SheetName.Trim() == sheetName.Trim())
                .ToList();

            if (tpSheets.Count == 0)
                return BadRequest(new
                {
                    message = string.IsNullOrWhiteSpace(sheetName)
                        ? "В файле не найдено листов с названием ТП-* (например, ТП-001)"
                        : $"Лист «{sheetName}» не найден или не является техпроцессом"
                });

            var tpFolderNode = await GetOrCreateTpFolderNodeAsync();

            var existingKeys = (await _context.Agents
                    .Where(a => a.TechProcessId != null)
                    .Select(a => new { a.TechProcess!.Code, a.TechProcess.Name })
                    .ToListAsync())
                .Select(k => TechProcessKey(k.Code, k.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var createdAgents = new List<AgentDto>();
            var skipped = new List<string>();
            var emptySheets = new List<string>();
            var totalOperations = 0;
            var queueBase = GetNextQueueNumber();
            var strategy = _context.Database.CreateExecutionStrategy();

            foreach (var sheet in tpSheets)
            {
                if (sheet.Operations.Count == 0)
                {
                    emptySheets.Add(sheet.SheetName);
                    continue;
                }

                if (!existingKeys.Add(TechProcessKey(sheet.Code, sheet.Name)))
                {
                    skipped.Add(sheet.SheetName);
                    continue;
                }

                var queueNumber = queueBase + createdAgents.Count;
                var agentId = await strategy.ExecuteAsync(async () =>
                {
                    _context.ChangeTracker.Clear();
                    await using var tx = await _context.Database.BeginTransactionAsync();
                    var id = await CreateTechProcessAgentAsync(sheet, file.FileName, tpFolderNode.Id, queueNumber);
                    await tx.CommitAsync();
                    return id;
                });

                totalOperations += sheet.Operations.Count;
                createdAgents.Add(await BuildAgentDto(agentId));
            }

            return Ok(new
            {
                message = $"Создано {createdAgents.Count} техпроцессов ({totalOperations} операций) в папке «{TpFolderName}»" +
                          (skipped.Count > 0 ? $", пропущено уже загруженных: {skipped.Count}" : ""),
                tp_folder_id = tpFolderNode.Id,
                created_count = createdAgents.Count,
                operations_count = totalOperations,
                skipped_count = skipped.Count,
                skipped_sheets = skipped,
                empty_sheets = emptySheets,
                agents = createdAgents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching tech process");
            return StatusCode(500, new { message = "Error attaching tech process", error = ex.Message });
        }
    }

    private static string TechProcessKey(string code, string? name) => $"{code.Trim()}|{name?.Trim()}";

    private async Task<int> CreateTechProcessAgentAsync(ParsedTechProcess sheet, string fileName, string folderId, long queueNumber)
    {
        var techProcess = new TechProcess
        {
            Code = sheet.Code,
            Name = sheet.Name,
            SourceKind = "xlsx",
            SourceFileName = fileName,
            SourceSheetName = sheet.SheetName,
            ImportedAt = DateTime.UtcNow,
            Operations = sheet.Operations
                .Select((op, idx) => new TechProcessOperation
                {
                    OpNo = op.OpNo,
                    OpType = op.OpType,
                    BlankCode = op.BlankCode,
                    SetupTimeMin = op.SetupTimeMin,
                    ProcessTimeMin = op.ProcessTimeMin,
                    ServiceTimeMin = op.ServiceTimeMin,
                    EquipmentCode = op.EquipmentCode,
                    ToolCodes = op.ToolCodes.Count == 0 ? null : string.Join(";", op.ToolCodes),
                    ProfessionCode = op.ProfessionCode,
                    Description = op.Description,
                    Extra = op.Extra,
                    Position = idx
                })
                .ToList()
        };

        var product = sheet.Operations.Select(o => o.BlankCode).FirstOrDefault(b => !string.IsNullOrWhiteSpace(b))?.Trim();
        var productTitle = product ?? sheet.Name ?? string.Empty;
        var productName = !string.IsNullOrEmpty(sheet.Name) && productTitle.StartsWith(sheet.Name, StringComparison.OrdinalIgnoreCase)
            ? productTitle[sheet.Name.Length..].Trim()
            : productTitle;

        var defaultHostId = await _context.Hosts
            .OrderBy(h => h.Id)
            .Select(h => (int?)h.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Нет Host в системе. Создайте Host перед импортом ТП.");

        await _lifecycle.ValidateHostCapacityAsync(defaultHostId);

        var agent = new Agent
        {
            NodeId = folderId,
            HostId = defaultHostId,
            Name = $"{sheet.Code} {productTitle}".Trim(),
            Type = "tech_process",
            AgentTypeValue = "tech_process",
            TechProcess = techProcess,
            Quantity = 1,
            QueueNumber = queueNumber,
            WorkflowApplied = true,
            WorkflowStatus = "ready",
            AppliedWorkflows = JsonSerializer.Serialize(new List<string> { DefaultTpWorkflow })
        };

        void AddParam(string name, string? value, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            agent.Parameters.Add(new AgentParameter
            {
                Name = name,
                Value = value.Length > 255 ? value[..255] : value,
                Description = description
            });
        }

        static string JoinDistinct(IEnumerable<string?> values) =>
            string.Join("; ", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

        AddParam("Номер ТП", sheet.Code);
        AddParam("Обозначение", sheet.Name);
        AddParam("Изделие", productName);
        AddParam("Количество операций", sheet.Operations.Count.ToString(), "Все операции сохранены в tech_process_operations");
        AddParam("Операции", JoinDistinct(sheet.Operations.Select(o => o.OpNo?.ToString("000"))));
        AddParam("Коды оборудования", JoinDistinct(sheet.Operations.Select(o => o.EquipmentCode)));
        AddParam("Коды профессий", JoinDistinct(sheet.Operations.Select(o => o.ProfessionCode)));
        AddParam("Коды инструментов", JoinDistinct(sheet.Operations.SelectMany(o => o.ToolCodes)));
        AddParam("Лист XLSX", sheet.SheetName);

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        try
        {
            var loaded = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .FirstAsync(a => a.Id == agent.Id);
            await _lifecycle.ProvisionAgentAsync(loaded);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TP agent {Id} created but provision failed", agent.Id);
        }

        // camelCase: движок workflow и шаблоны ждут techProcessId / wfId
        var payloadObj = new Dictionary<string, object?>
        {
            ["techProcessId"] = techProcess.Id,
            ["techProcessCode"] = techProcess.Code,
            ["techProcessName"] = techProcess.Name,
            ["operations"] = sheet.Operations.Select(o => new Dictionary<string, object?>
            {
                ["opNo"] = o.OpNo,
                ["opType"] = o.OpType,
                ["blankCode"] = o.BlankCode,
                ["setupTimeMin"] = o.SetupTimeMin,
                ["processTimeMin"] = o.ProcessTimeMin,
                ["serviceTimeMin"] = o.ServiceTimeMin,
                ["equipmentCode"] = o.EquipmentCode,
                ["toolCodes"] = o.ToolCodes,
                ["professionCode"] = o.ProfessionCode,
                ["description"] = o.Description,
                ["extra"] = o.Extra
            }).ToList()
        };

        _context.PendingMessages.Add(new PendingMessage
        {
            AgentId = agent.Id,
            Kind = "work",
            WorkflowType = DefaultTpWorkflow,
            Payload = JsonSerializer.Serialize(payloadObj),
            Status = "draft"
        });
        await _context.SaveChangesAsync();

        return agent.Id;
    }

    // POST api/tech-process/start/{agentId}
    [HttpPost("start/{agentId:int}")]
    public async Task<IActionResult> Start(int agentId)
    {
        try
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == agentId);
            if (agent == null)
                return NotFound(new { message = $"Agent with id {agentId} not found" });

            // find latest draft
            var pending = await _context.PendingMessages
                .Where(pm => pm.AgentId == agentId && pm.Status == "draft")
                .OrderByDescending(pm => pm.Id)
                .FirstOrDefaultAsync();

            if (pending == null)
                return NotFound(new { message = "No draft pending message for this agent" });

            var agentRoutingKey = $"agent.{agent.Id}";
            var parameters = Backend.Services.Workflow.WorkflowParameterNormalizer.FromJson(
                pending.Payload,
                techProcessId: agent.TechProcessId);

            var workMsg = new WorkMessage(
                messageType: pending.WorkflowType,
                senderId: "ui",
                receiverId: agentRoutingKey,
                parameter: parameters
            );

            _rabbitMQService.SendMessage(workMsg, workMsg.ReceiverId, workMsg.MessageType);

            pending.Status = "sent";
            pending.SentAt = DateTime.UtcNow;
            agent.WorkflowStatus = "running";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Started",
                pending_message_id = pending.Id,
                agent_id = agentRoutingKey,
                workflow_type = pending.WorkflowType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting tech process for agent {AgentId}", agentId);
            return StatusCode(500, new { message = "Error starting tech process" });
        }
    }

    // GET api/tech-process/agent/{agentId}
    [HttpGet("agent/{agentId:int}")]
    public async Task<ActionResult<object>> GetForAgent(int agentId)
    {
        var agent = await _context.Agents
            .Include(a => a.TechProcess)
            .Include(a => a.PendingMessages)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agent == null)
            return NotFound(new { message = $"Agent with id {agentId} not found" });

        return Ok(await BuildAgentDto(agent.Id));
    }

    // GET api/tech-process/agent/{agentId}/operations
    [HttpGet("agent/{agentId:int}/operations")]
    public async Task<ActionResult<object>> GetOperationsForAgent(int agentId)
    {
        var agent = await _context.Agents
            .Include(a => a.TechProcess)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agent == null)
            return NotFound(new { message = $"Agent with id {agentId} not found" });
        if (agent.TechProcess == null)
            return NotFound(new { message = "К агенту не привязан техпроцесс" });

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == agent.TechProcess.Id)
            .OrderBy(o => o.Position)
            .Select(o => new
            {
                id = o.Id,
                op_no = o.OpNo,
                op_type = o.OpType,
                blank_code = o.BlankCode,
                setup_time_min = o.SetupTimeMin,
                process_time_min = o.ProcessTimeMin,
                service_time_min = o.ServiceTimeMin,
                equipment_code = o.EquipmentCode,
                tool_codes = o.ToolCodes,
                profession_code = o.ProfessionCode,
                description = o.Description,
                extra = o.Extra
            })
            .ToListAsync();

        return Ok(new
        {
            tech_process_id = agent.TechProcess.Id,
            code = agent.TechProcess.Code,
            name = agent.TechProcess.Name,
            source_sheet_name = agent.TechProcess.SourceSheetName,
            operations
        });
    }

    private async Task<AgentDto> BuildAgentDto(int agentId)
    {
        var agent = await _context.Agents
            .Include(a => a.Parameters)
            .Include(a => a.Host)
            .Include(a => a.TechProcess)
            .Include(a => a.PendingMessages)
            .FirstAsync(a => a.Id == agentId);

        var latestDraft = agent.PendingMessages
            .Where(pm => pm.Status == "draft")
            .OrderByDescending(pm => pm.Id)
            .FirstOrDefault();

        PendingMessageDto? pendingDto = null;
        if (latestDraft != null)
        {
            pendingDto = new PendingMessageDto
            {
                Id = latestDraft.Id,
                AgentId = latestDraft.AgentId,
                Kind = latestDraft.Kind,
                WorkflowType = latestDraft.WorkflowType,
                Status = latestDraft.Status,
                CreatedAt = latestDraft.CreatedAt,
                SentAt = latestDraft.SentAt,
                Payload = JsonSerializer.Deserialize<object>(latestDraft.Payload) ?? new()
            };
        }

        return new AgentDto
        {
            Id = agent.Id,
            NodeId = agent.NodeId,
            HostId = agent.HostId,
            HostName = agent.Host?.Name,
            Name = agent.Name,
            Type = agent.Type,
            Profession = agent.Profession,
            Status = agent.Status,
            Quantity = agent.Quantity,
            QueueNumber = agent.QueueNumber,
            Parameters = agent.Parameters.Select(p => new AgentParameterDto
            {
                Id = p.Id,
                AgentId = p.AgentId,
                Name = p.Name,
                Unit = p.Unit,
                Value = p.Value,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList(),
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt,
            WorkflowApplied = agent.WorkflowApplied,
            WorkflowStatus = agent.WorkflowStatus,
            AppliedWorkflows = System.Text.Json.JsonSerializer.Deserialize<List<string>>(agent.AppliedWorkflows) ?? new(),
            TechProcessId = agent.TechProcessId,
            PendingMessage = pendingDto
        };
    }

    // Парсит все листы из XLSX файла
    private List<ParsedTechProcess> ParseAllSheetsFromXlsx(IFormFile file)
    {
        var result = new List<ParsedTechProcess>();

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = file.OpenReadStream();
        using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration
        {
            ConfigureDataTable = tableReader => new ExcelDataReader.ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        // Для каждого листа парсим данные
        foreach (DataTable table in dataSet.Tables)
        {
            if (!IsTpSheet(table.TableName))
                continue;

            try
            {
                // Парсим операции из листа
                var parsed = ParseSheetData(table);
                if (parsed != null)
                {
                    result.Add(parsed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing sheet {SheetName}", table.TableName);
            }
        }

        return result;
    }

    // Парсит данные одного листа
    private ParsedTechProcess? ParseSheetData(DataTable table)
    {
        var sheetName = table.TableName;

        // Извлекаем код и имя из названия листа
        var codeMatch = System.Text.RegularExpressions.Regex.Match(sheetName, @"^(ТП-\d+)");
        var code = codeMatch.Success ? codeMatch.Groups[1].Value : sheetName;

        var nameMatch = System.Text.RegularExpressions.Regex.Match(sheetName, @"«(?<n>[^»]+)»");
        var name = nameMatch.Success ? nameMatch.Groups["n"].Value.Trim() : null;

        // Находим заголовок (строку с заголовками столбцов)
        int headerRowIndex = -1;
        const string H_OpNo = "Номер операции";
        const string H_OpType = "Тип операции";
        const string H_Desc = "Описание операции";

        for (int i = 0; i < Math.Min(10, table.Rows.Count); i++)
        {
            var rowValues = table.Rows[i].ItemArray.Select(v => v?.ToString()?.Trim()).ToList();
            if (rowValues.Contains(H_OpNo) && rowValues.Contains(H_OpType) && rowValues.Contains(H_Desc))
            {
                headerRowIndex = i;
                break;
            }
        }

        if (headerRowIndex < 0)
        {
            _logger.LogWarning("Header row not found in sheet {SheetName}", sheetName);
            return null;
        }

        // Создаем карту заголовков
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < table.Rows[headerRowIndex].ItemArray.Length; c++)
        {
            var key = table.Rows[headerRowIndex][c]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(key) && !headerMap.ContainsKey(key))
                headerMap[key] = c;
        }

        var parsed = new ParsedTechProcess
        {
            SheetName = sheetName,
            Code = code,
            Name = name
        };

        // Парсим операции
        for (int r = headerRowIndex + 1; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];

            // Пропускаем пустые строки
            if (row.ItemArray.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString())))
                continue;

            var opNoStr = GetCellValue(row, headerMap, H_OpNo);
            var opNo = TryParseInt(opNoStr);
            var desc = GetCellValue(row, headerMap, H_Desc);

            // Требует хотя бы номер операции или описание
            if (opNo == null && string.IsNullOrWhiteSpace(desc))
                continue;

            parsed.Operations.Add(new ParsedTechProcessOperation
            {
                OpNo = opNo,
                OpType = NullIfEmpty(GetCellValue(row, headerMap, H_OpType)),
                BlankCode = NullIfEmpty(GetCellValue(row, headerMap, "Заготовка")),
                SetupTimeMin = TryParseDouble(GetCellValue(row, headerMap, "Время наладки оборудования для операции, мин")),
                ProcessTimeMin = TryParseDouble(GetCellValue(row, headerMap, "Время операции, мин")),
                ServiceTimeMin = TryParseDouble(GetCellValue(row, headerMap, "Время обслуживания оборудования после операции, мин")),
                EquipmentCode = NullIfEmpty(GetCellValue(row, headerMap, "Код необходимого оборудования")),
                ToolCodes = SplitCodes(GetCellValue(row, headerMap, "Коды необходимых инструментов")),
                ProfessionCode = NullIfEmpty(GetCellValue(row, headerMap, "Код необходимой профессии")),
                Description = NullIfEmpty(desc),
                Extra = NullIfEmpty(GetCellValue(row, headerMap, "Дополнительные характеристики"))
            });
        }

        return parsed;
    }

    private string GetCellValue(DataRow row, Dictionary<string, int> headerMap, string header)
    {
        if (!headerMap.TryGetValue(header, out var idx))
            return string.Empty;
        if (idx < 0 || idx >= row.ItemArray.Length)
            return string.Empty;
        return row[idx]?.ToString()?.Trim() ?? string.Empty;
    }

    private string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private int? TryParseInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (int.TryParse(s.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        if (double.TryParse(s.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dv))
            return (int)dv;
        return null;
    }

    private double? TryParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (double.TryParse(s.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    private List<string> SplitCodes(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<string>();
        return System.Text.RegularExpressions.Regex.Split(s, "[;,\r\n\t ]+")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Проверяет, является ли лист техпроцессом (ТП-*)
    private bool IsTpSheet(string sheetName)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(sheetName.Trim(), @"^ТП-\d+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // Находит или создает единую папку техпроцессов (старая папка "ТП" переименовывается)
    private async Task<TreeNode> GetOrCreateTpFolderNodeAsync()
    {
        var candidates = await _context.TreeNodes
            .Where(t => t.Type == "branch" && t.ParentId == null
                        && (t.Name == TpFolderName || t.Name == LegacyTpFolderName))
            .ToListAsync();

        var tpFolder = candidates.FirstOrDefault(t => t.Name == TpFolderName)
                       ?? candidates.FirstOrDefault();

        if (tpFolder != null)
        {
            if (tpFolder.Name != TpFolderName)
            {
                tpFolder.Name = TpFolderName;
                await _context.SaveChangesAsync();
            }
            return tpFolder;
        }

        tpFolder = new TreeNode
        {
            Id = $"tp-folder-{Guid.NewGuid():N}",
            Name = TpFolderName,
            Icon = "📁",
            Type = "branch",
            ParentId = null,
            Position = 999 // В конец списка
        };

        _context.TreeNodes.Add(tpFolder);
        await _context.SaveChangesAsync();

        return tpFolder;
    }

    // Генерирует следующий номер очереди
    private long GetNextQueueNumber()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
