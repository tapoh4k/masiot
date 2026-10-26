using Backend.Data;
using Backend.Models;
using Backend.Services.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для процесса планирования (WF1-WF5.2)
/// Категория: Planning
/// </summary>
public class PlanningFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<PlanningFunctions> _logger;

    public PlanningFunctions(
        ApplicationDbContext context,
        IRabbitMQService rabbitMQService,
        ILogger<PlanningFunctions> logger)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    /// <summary>
    /// Подготовить техпроцесс и определить первую операцию
    /// Параметры: techProcessId (int)
    /// Возвращает: nextOperation (int) - номер первой операции
    /// </summary>
    public async Task<int> TechprocessPrepare(int techProcessId)
    {
        _logger.LogInformation("Preparing tech process {TechProcessId}", techProcessId);

        var operations = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId)
            .OrderBy(o => o.Position)
            .ToListAsync();

        if (operations.Count == 0)
        {
            _logger.LogWarning("No operations found for tech process {TechProcessId}", techProcessId);
            return -1;
        }

        var firstOp = operations.First();
        return firstOp.OpNo ?? 0;
    }

    /// <summary>
    /// Подготовить сообщения ресурсам о работе
    /// Параметры: techProcessId (int), opNo (int), workMessageType (string)
    /// Возвращает: messages (string) - JSON array сообщений
    /// </summary>
    public async Task<string> PrepareMessagesToAgentAboutJob(int techProcessId, int opNo, string workMessageType)
    {
        _logger.LogInformation("Preparing messages for operation {OpNo} of tech process {TechProcessId}",
            opNo, techProcessId);

        var operation = await _context.TechProcessOperations
            .FirstOrDefaultAsync(o => o.TechProcessId == techProcessId && o.OpNo == opNo);

        if (operation == null)
        {
            _logger.LogWarning("Operation {OpNo} not found", opNo);
            return "[]";
        }

        var messages = new List<object>();

        // Prepare message for equipment if specified
        if (!string.IsNullOrEmpty(operation.EquipmentCode))
        {
            messages.Add(new
            {
                messageType = workMessageType,
                routingKey = $"okpd2.{operation.EquipmentCode}",
                payload = new
                {
                    tech_process_id = techProcessId,
                    operation_number = opNo,
                    operation_type = operation.OpType,
                    setup_time_min = operation.SetupTimeMin,
                    process_time_min = operation.ProcessTimeMin,
                    service_time_min = operation.ServiceTimeMin,
                    equipment_code = operation.EquipmentCode,
                    request_type = "equipment"
                }
            });
        }

        // Prepare message for profession if specified
        if (!string.IsNullOrEmpty(operation.ProfessionCode))
        {
            messages.Add(new
            {
                messageType = workMessageType,
                routingKey = $"okpd2.{operation.ProfessionCode}",
                payload = new
                {
                    tech_process_id = techProcessId,
                    operation_number = opNo,
                    operation_type = operation.OpType,
                    setup_time_min = operation.SetupTimeMin,
                    process_time_min = operation.ProcessTimeMin,
                    service_time_min = operation.ServiceTimeMin,
                    profession_code = operation.ProfessionCode,
                    request_type = "profession"
                }
            });
        }

        return JsonConvert.SerializeObject(messages);
    }

    /// <summary>
    /// Подготовить сообщение для СПР (системы принятия решений)
    /// Параметры: techProcessId (int), opNo (int), typePlan (string), wfId (int)
    /// Возвращает: message (string) - JSON сообщения
    /// </summary>
    public async Task<string> PrepareMessageToSPR(int techProcessId, int opNo, string typePlan, int wfId)
    {
        _logger.LogInformation("Preparing message to SPR for operation {OpNo}", opNo);

        // Get all candidate responses from timetable
        var candidates = await _context.Timetables
            .Where(t => t.WFId == wfId && t.Status == 20) // Status 20 = candidate
            .ToListAsync();

        var message = new
        {
            messageType = "toSPR",
            routingKey = "spr.decision",
            payload = new
            {
                tech_process_id = techProcessId,
                operation_number = opNo,
                type_plan = typePlan,
                wf_id = wfId,
                candidates = candidates.Select(c => new
                {
                    timetable_id = c.Id,
                    partner_id = c.PartnerId,
                    time_start = c.TimeStart,
                    time_end = c.TimeEnd,
                    quantity = c.Quantity,
                    location = c.Location
                }).ToList()
            }
        };

        return JsonConvert.SerializeObject(message);
    }

    /// <summary>
    /// Добавить событие в расписание (timetable)
    /// Параметры: orderNumber (string), operationNumber (int), timeStart (string - ISO),
    ///            length (int), timeEnd (string - ISO), quantity (double), location (string),
    ///            candidateStatus (int), partnerId (string), wfId (int), parameters (string - JSON)
    /// Возвращает: timetableId (int)
    /// </summary>
    public async Task<int> AddTimetableEvent(
        string orderNumber,
        int operationNumber,
        string timeStart,
        int length,
        string timeEnd,
        double quantity,
        string location,
        int candidateStatus,
        string partnerId,
        int wfId,
        string parameters)
    {
        _logger.LogInformation("Adding timetable event for order {OrderNumber}, operation {OperationNumber}",
            orderNumber, operationNumber);

        var entry = new Timetable
        {
            OrderNumber = orderNumber,
            TimeStart = DateTime.Parse(timeStart),
            TimeEnd = DateTime.Parse(timeEnd),
            Quantity = quantity,
            Location = location,
            Status = candidateStatus,
            PartnerId = partnerId,
            WFId = wfId,
            Parameters = parameters
        };

        _context.Timetables.Add(entry);
        await _context.SaveChangesAsync();

        return entry.Id;
    }

    /// <summary>
    /// Обработать ответ СПР
    /// Параметры: wfId (int), result (bool), orderNumber (string), operationNumber (int), shiftTime (int)
    /// Возвращает: status (string) - "approved" или "rejected"
    /// </summary>
    public async Task<string> ProcessingSPRResponse(
        int wfId,
        bool result,
        string orderNumber,
        int operationNumber,
        int shiftTime)
    {
        _logger.LogInformation("Processing SPR response for WF {WfId}: {Result}", wfId, result);

        var candidates = await _context.Timetables
            .Where(t => t.WFId == wfId && t.OrderNumber == orderNumber && t.Status == 20)
            .ToListAsync();

        if (result)
        {
            // Approved - update candidates to confirmed status
            foreach (var candidate in candidates)
            {
                candidate.Status = 30; // Confirmed
            }

            _logger.LogInformation("Approved {Count} candidates", candidates.Count);
        }
        else
        {
            // Rejected - update candidates to rejected status and shift time
            foreach (var candidate in candidates)
            {
                candidate.Status = 40; // Rejected
                candidate.TimeStart = candidate.TimeStart.AddMinutes(shiftTime);
                candidate.TimeEnd = candidate.TimeEnd.AddMinutes(shiftTime);
            }

            _logger.LogInformation("Rejected {Count} candidates, shifted by {ShiftTime} minutes",
                candidates.Count, shiftTime);
        }

        await _context.SaveChangesAsync();

        return result ? "approved" : "rejected";
    }

    /// <summary>
    /// Получить время окончания операции
    /// Параметры: orderNumber (string), operationNumber (int)
    /// Возвращает: timeEnd (string) - ISO datetime
    /// </summary>
    public async Task<string> GetOperationTimeEnd(string orderNumber, int operationNumber)
    {
        _logger.LogInformation("Getting time end for operation {OperationNumber} of order {OrderNumber}",
            operationNumber, orderNumber);

        var entry = await _context.Timetables
            .Where(t => t.OrderNumber == orderNumber && t.Status == 30) // Confirmed
            .OrderByDescending(t => t.TimeEnd)
            .FirstOrDefaultAsync();

        if (entry == null)
        {
            _logger.LogWarning("No confirmed timetable entry found");
            return DateTime.UtcNow.ToString("o");
        }

        return entry.TimeEnd.ToString("o");
    }

    /// <summary>
    /// Проверить существование следующей операции
    /// Параметры: techProcessId (int), currentOpNo (int)
    /// Возвращает: result (string) - JSON с полями exist (bool) и nextOperation (int)
    /// </summary>
    public async Task<string> NextOperationExist(int techProcessId, int currentOpNo)
    {
        _logger.LogInformation("Checking if next operation exists after {CurrentOpNo}", currentOpNo);

        var currentOp = await _context.TechProcessOperations
            .FirstOrDefaultAsync(o => o.TechProcessId == techProcessId && o.OpNo == currentOpNo);

        if (currentOp == null)
        {
            return JsonConvert.SerializeObject(new { exist = false, nextOperation = -1 });
        }

        var nextOp = await _context.TechProcessOperations
            .Where(o => o.TechProcessId == techProcessId && o.Position > currentOp.Position)
            .OrderBy(o => o.Position)
            .FirstOrDefaultAsync();

        if (nextOp == null)
        {
            return JsonConvert.SerializeObject(new { exist = false, nextOperation = -1 });
        }

        return JsonConvert.SerializeObject(new { exist = true, nextOperation = nextOp.OpNo ?? -1 });
    }

    /// <summary>
    /// Обновить время начала операции
    /// Параметры: orderNumber (string), operationNumber (int), timeEnd (string - ISO), deltaTime (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public string UpdateOperationTimeStart(
        string orderNumber,
        int operationNumber,
        string timeEnd,
        int deltaTime)
    {
        _logger.LogInformation("Updating time start for operation {OperationNumber}", operationNumber);

        var endTime = DateTime.Parse(timeEnd);
        var newStartTime = endTime.AddMinutes(deltaTime);

        // This would typically update a planning record
        // For now, just log the action
        _logger.LogInformation("Operation {OperationNumber} will start at {StartTime}",
            operationNumber, newStartTime);

        return "true";
    }
}
