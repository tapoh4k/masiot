using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для работы с техпроцессами
/// Категория: TechProcess
/// </summary>
public class TechProcessFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TechProcessFunctions> _logger;

    public TechProcessFunctions(ApplicationDbContext context, ILogger<TechProcessFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить техпроцесс по ID
    /// Параметры: techProcessId (int)
    /// Возвращает: techProcess (string) - JSON
    /// </summary>
    public async Task<string> GetTechProcess(int techProcessId)
    {
        _logger.LogInformation("Getting tech process {TechProcessId}", techProcessId);

        var tp = await _context.TechProcesses
            .Include(t => t.Operations)
            .FirstOrDefaultAsync(t => t.Id == techProcessId);

        if (tp == null)
        {
            _logger.LogWarning("Tech process {TechProcessId} not found", techProcessId);
            return "{}";
        }

        var result = new
        {
            tp.Id,
            tp.Code,
            tp.Name,
            tp.SourceKind,
            tp.SourceFileName,
            tp.SourceSheetName,
            tp.ImportedAt,
            Operations = tp.Operations.OrderBy(o => o.Position).Select(o => new
            {
                o.Id,
                o.OpNo,
                o.OpType,
                o.BlankCode,
                o.SetupTimeMin,
                o.ProcessTimeMin,
                o.ServiceTimeMin,
                o.EquipmentCode,
                o.ToolCodes,
                o.ProfessionCode,
                o.Description,
                o.Extra
            }).ToList()
        };

        return JsonConvert.SerializeObject(result);
    }

    /// <summary>
    /// Получить операцию техпроцесса по номеру
    /// Параметры: techProcessId (int), opNo (int)
    /// Возвращает: operation (string) - JSON
    /// </summary>
    public async Task<string> GetOperationByNumber(int techProcessId, int opNo)
    {
        _logger.LogInformation("Getting operation {OpNo} from tech process {TechProcessId}", opNo, techProcessId);

        var operation = await _context.TechProcessOperations
            .FirstOrDefaultAsync(o => o.TechProcessId == techProcessId && o.OpNo == opNo);

        if (operation == null)
        {
            _logger.LogWarning("Operation {OpNo} not found in tech process {TechProcessId}", opNo, techProcessId);
            return "{}";
        }

        var result = new
        {
            operation.Id,
            operation.OpNo,
            operation.OpType,
            operation.BlankCode,
            operation.SetupTimeMin,
            operation.ProcessTimeMin,
            operation.ServiceTimeMin,
            operation.EquipmentCode,
            operation.ToolCodes,
            operation.ProfessionCode,
            operation.Description,
            operation.Extra
        };

        return JsonConvert.SerializeObject(result);
    }

    /// <summary>
    /// Получить все операции техпроцесса
    /// Параметры: techProcessId (int)
    /// Возвращает: operations (string) - JSON array
    /// </summary>
    public async Task<string> GetAllOperations(int techProcessId)
    {
        _logger.LogInformation("Getting all operations for tech process {TechProcessId}", techProcessId);

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId)
            .OrderBy(o => o.Position)
            .Select(o => new
            {
                o.Id,
                o.OpNo,
                o.OpType,
                o.BlankCode,
                o.SetupTimeMin,
                o.ProcessTimeMin,
                o.ServiceTimeMin,
                o.EquipmentCode,
                o.ToolCodes,
                o.ProfessionCode,
                o.Description,
                o.Extra
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(operations);
    }

    /// <summary>
    /// Получить операции по коду оборудования
    /// Параметры: techProcessId (int), equipmentCode (string)
    /// Возвращает: operations (string) - JSON array
    /// </summary>
    public async Task<string> GetOperationsByEquipment(int techProcessId, string equipmentCode)
    {
        _logger.LogInformation("Getting operations for equipment {EquipmentCode} in tech process {TechProcessId}",
            equipmentCode, techProcessId);

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && o.EquipmentCode == equipmentCode)
            .OrderBy(o => o.Position)
            .Select(o => new
            {
                o.Id,
                o.OpNo,
                o.OpType,
                o.SetupTimeMin,
                o.ProcessTimeMin,
                o.ServiceTimeMin,
                o.Description
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(operations);
    }

    /// <summary>
    /// Получить операции по коду профессии
    /// Параметры: techProcessId (int), professionCode (string)
    /// Возвращает: operations (string) - JSON array
    /// </summary>
    public async Task<string> GetOperationsByProfession(int techProcessId, string professionCode)
    {
        _logger.LogInformation("Getting operations for profession {ProfessionCode} in tech process {TechProcessId}",
            professionCode, techProcessId);

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && o.ProfessionCode == professionCode)
            .OrderBy(o => o.Position)
            .Select(o => new
            {
                o.Id,
                o.OpNo,
                o.OpType,
                o.SetupTimeMin,
                o.ProcessTimeMin,
                o.ServiceTimeMin,
                o.Description
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(operations);
    }

    /// <summary>
    /// Рассчитать общее время выполнения техпроцесса
    /// Параметры: techProcessId (int)
    /// Возвращает: totalTimeMin (double)
    /// </summary>
    public async Task<double> CalculateTotalTime(int techProcessId)
    {
        _logger.LogInformation("Calculating total time for tech process {TechProcessId}", techProcessId);

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId)
            .ToListAsync();

        var totalTime = operations.Sum(o =>
            (o.SetupTimeMin ?? 0) +
            (o.ProcessTimeMin ?? 0) +
            (o.ServiceTimeMin ?? 0));

        return totalTime;
    }

    /// <summary>
    /// Получить время выполнения операции (наладка + обработка + обслуживание)
    /// Параметры: techProcessId (int), opNo (int)
    /// Возвращает: minutes (double), 0 если операция не найдена
    /// </summary>
    public async Task<double> GetOperationTime(int techProcessId, int opNo)
    {
        _logger.LogInformation("Getting time of operation {OpNo} in tech process {TechProcessId}", opNo, techProcessId);

        var operation = await _context.TechProcessOperations
            .FirstOrDefaultAsync(o => o.TechProcessId == techProcessId && o.OpNo == opNo);

        if (operation == null)
        {
            _logger.LogWarning("Operation {OpNo} not found in tech process {TechProcessId}", opNo, techProcessId);
            return 0;
        }

        return (operation.SetupTimeMin ?? 0) + (operation.ProcessTimeMin ?? 0) + (operation.ServiceTimeMin ?? 0);
    }

    /// <summary>
    /// Получить код оборудования операции
    /// Параметры: techProcessId (int), opNo (int)
    /// Возвращает: equipmentCode (string), пустая строка если не найден
    /// </summary>
    public async Task<string> GetEquipmentCode(int techProcessId, int opNo)
    {
        _logger.LogInformation("Getting equipment code of operation {OpNo} in tech process {TechProcessId}", opNo, techProcessId);

        var code = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && o.OpNo == opNo)
            .Select(o => o.EquipmentCode)
            .FirstOrDefaultAsync();

        return code ?? "";
    }

    /// <summary>
    /// Получить код профессии операции
    /// Параметры: techProcessId (int), opNo (int)
    /// Возвращает: professionCode (string), пустая строка если не найден
    /// </summary>
    public async Task<string> GetProfessionCode(int techProcessId, int opNo)
    {
        _logger.LogInformation("Getting profession code of operation {OpNo} in tech process {TechProcessId}", opNo, techProcessId);

        var code = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && o.OpNo == opNo)
            .Select(o => o.ProfessionCode)
            .FirstOrDefaultAsync();

        return code ?? "";
    }

    /// <summary>
    /// Получить количество операций в техпроцессе
    /// Параметры: techProcessId (int)
    /// Возвращает: count (int)
    /// </summary>
    public async Task<int> GetOperationsCount(int techProcessId)
    {
        _logger.LogInformation("Counting operations in tech process {TechProcessId}", techProcessId);

        return await _context.TechProcessOperations
            .CountAsync(o => o.TechProcessId == techProcessId);
    }

    /// <summary>
    /// Получить список уникальных кодов оборудования в техпроцессе
    /// Параметры: techProcessId (int)
    /// Возвращает: equipmentCodes (string) - JSON array
    /// </summary>
    public async Task<string> GetUniqueEquipmentCodes(int techProcessId)
    {
        _logger.LogInformation("Getting unique equipment codes for tech process {TechProcessId}", techProcessId);

        var codes = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && !string.IsNullOrEmpty(o.EquipmentCode))
            .Select(o => o.EquipmentCode)
            .Distinct()
            .ToListAsync();

        return JsonConvert.SerializeObject(codes);
    }

    /// <summary>
    /// Получить список уникальных кодов профессий в техпроцессе
    /// Параметры: techProcessId (int)
    /// Возвращает: professionCodes (string) - JSON array
    /// </summary>
    public async Task<string> GetUniqueProfessionCodes(int techProcessId)
    {
        _logger.LogInformation("Getting unique profession codes for tech process {TechProcessId}", techProcessId);

        var codes = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && !string.IsNullOrEmpty(o.ProfessionCode))
            .Select(o => o.ProfessionCode)
            .Distinct()
            .ToListAsync();

        return JsonConvert.SerializeObject(codes);
    }
}
