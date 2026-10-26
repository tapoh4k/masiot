using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MonitoringController> _logger;
    private readonly IConfiguration _configuration;

    public MonitoringController(
        ApplicationDbContext context,
        ILogger<MonitoringController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // Middleware для проверки API ключа (constant-time comparison)
    private bool ValidateApiKey()
    {
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        var validApiKey = _configuration["MonitoringApiKey"];
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(validApiKey))
        {
            return false;
        }
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(apiKey),
            System.Text.Encoding.UTF8.GetBytes(validApiKey)
        );
    }

    // GET: api/monitoring/statistics
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        if (!ValidateApiKey())
        {
            return Unauthorized(new { message = "Invalid API key" });
        }

        try
        {
            var totalAgents = await _context.Agents.CountAsync();
            var techProcessCount = await _context.Agents.CountAsync(a => a.Type == "tech_process" || a.Type == "Тех процесс");
            var equipmentCount = await _context.Agents.CountAsync(a => a.Type == "equipment" || a.Type == "Оборудование");
            var instrumentCount = await _context.Agents.CountAsync(a => a.Type == "instrument" || a.Type == "Инструмент");
            var personnelCount = await _context.Agents.CountAsync(a => a.Type == "personnel" || a.Type == "Персонал");
            var blankCount = await _context.Agents.CountAsync(a => a.Type == "blank" || a.Type == "Заготовки");
            var usersCount = await _context.Users.CountAsync();
            var treeNodesCount = await _context.TreeNodes.CountAsync();

            var result = new
            {
                timestamp = DateTime.UtcNow,
                agents = new
                {
                    total = totalAgents,
                    tech_process = techProcessCount,
                    equipment = equipmentCount,
                    instrument = instrumentCount,
                    personnel = personnelCount,
                    blank = blankCount
                },
                users_count = usersCount,
                tree_nodes_count = treeNodesCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring statistics");
            return StatusCode(500, new { message = "Error retrieving statistics" });
        }
    }
}
