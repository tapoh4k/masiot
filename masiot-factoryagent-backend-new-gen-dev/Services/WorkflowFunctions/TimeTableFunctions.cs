using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для работы с расписанием (timetable)
/// Категория: TimeTable
/// </summary>
public class TimeTableFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimeTableFunctions> _logger;

    public TimeTableFunctions(ApplicationDbContext context, ILogger<TimeTableFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Создать запись в расписании
    /// Параметры: orderNumber (string), timeStart (string - ISO), timeEnd (string - ISO),
    ///            quantity (double), location (string), partnerId (string)
    /// Возвращает: timetableId (int)
    /// </summary>
    public async Task<int> CreateScheduleEntry(
        string orderNumber,
        string timeStart,
        string timeEnd,
        double quantity,
        string location,
        string partnerId)
    {
        _logger.LogInformation("Creating schedule entry for order {OrderNumber}", orderNumber);

        var entry = new Timetable
        {
            OrderNumber = orderNumber,
            TimeStart = DateTime.Parse(timeStart),
            TimeEnd = DateTime.Parse(timeEnd),
            Quantity = quantity,
            Location = location,
            PartnerId = partnerId,
            Status = 0
        };

        _context.Timetables.Add(entry);
        await _context.SaveChangesAsync();

        return entry.Id;
    }

    /// <summary>
    /// Получить расписание по номеру заказа
    /// Параметры: orderNumber (string)
    /// Возвращает: schedule (string) - JSON array
    /// </summary>
    public async Task<string> GetScheduleByOrder(string orderNumber)
    {
        _logger.LogInformation("Getting schedule for order {OrderNumber}", orderNumber);

        var entries = await _context.Timetables
            .Where(t => t.OrderNumber == orderNumber)
            .OrderBy(t => t.TimeStart)
            .Select(t => new
            {
                t.Id,
                t.OrderNumber,
                t.TimeStart,
                t.TimeEnd,
                t.Quantity,
                t.Location,
                t.Status,
                t.PartnerId
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(entries);
    }

    /// <summary>
    /// Обновить статус записи расписания
    /// Параметры: timetableId (int), status (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateScheduleStatus(int timetableId, int status)
    {
        _logger.LogInformation("Updating schedule {TimetableId} status to {Status}", timetableId, status);

        var entry = await _context.Timetables.FindAsync(timetableId);

        if (entry == null)
        {
            _logger.LogWarning("Timetable entry {TimetableId} not found", timetableId);
            return "false";
        }

        entry.Status = status;
        await _context.SaveChangesAsync();

        return "true";
    }

    /// <summary>
    /// Получить расписание по партнеру
    /// Параметры: partnerId (string)
    /// Возвращает: schedule (string) - JSON array
    /// </summary>
    public async Task<string> GetScheduleByPartner(string partnerId)
    {
        _logger.LogInformation("Getting schedule for partner {PartnerId}", partnerId);

        var entries = await _context.Timetables
            .Where(t => t.PartnerId == partnerId)
            .OrderBy(t => t.TimeStart)
            .Select(t => new
            {
                t.Id,
                t.OrderNumber,
                t.TimeStart,
                t.TimeEnd,
                t.Quantity,
                t.Location,
                t.Status
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(entries);
    }

    /// <summary>
    /// Получить расписание за период
    /// Параметры: startDate (string - ISO), endDate (string - ISO)
    /// Возвращает: schedule (string) - JSON array
    /// </summary>
    public async Task<string> GetScheduleByPeriod(string startDate, string endDate)
    {
        _logger.LogInformation("Getting schedule for period {StartDate} - {EndDate}", startDate, endDate);

        var start = DateTime.Parse(startDate);
        var end = DateTime.Parse(endDate);

        var entries = await _context.Timetables
            .Where(t => t.TimeStart >= start && t.TimeEnd <= end)
            .OrderBy(t => t.TimeStart)
            .Select(t => new
            {
                t.Id,
                t.OrderNumber,
                t.TimeStart,
                t.TimeEnd,
                t.Quantity,
                t.Location,
                t.Status,
                t.PartnerId
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(entries);
    }
}
