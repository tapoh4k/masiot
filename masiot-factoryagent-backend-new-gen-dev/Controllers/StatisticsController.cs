using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(ApplicationDbContext context, ILogger<StatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/statistics
    [HttpGet]
    public async Task<ActionResult<StatisticsDto>> GetStatistics()
    {
        try
        {
            // Подсчитываем статистику в реальном времени из таблицы агентов
            var totalAgents = await _context.Agents.CountAsync();
            var techProcessCount = await _context.Agents.CountAsync(a => a.Type == "tech_process" || a.Type == "Тех процесс");
            var equipmentCount = await _context.Agents.CountAsync(a => a.Type == "equipment" || a.Type == "Оборудование");
            var instrumentCount = await _context.Agents.CountAsync(a => a.Type == "instrument" || a.Type == "Инструмент");
            var personnelCount = await _context.Agents.CountAsync(a => a.Type == "personnel" || a.Type == "Персонал");
            var blankCount = await _context.Agents.CountAsync(a => a.Type == "blank" || a.Type == "Заготовки");

            var dto = new StatisticsDto
            {
                Id = 1,
                TotalAgents = totalAgents,
                TechProcessCount = techProcessCount,
                EquipmentCount = equipmentCount,
                InstrumentCount = instrumentCount,
                PersonnelCount = personnelCount,
                BlankCount = blankCount,
                OtherCount = 0,
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
        }
    }
}
