using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для работы с информацией об агентах
/// Категория: AgentInfo
/// </summary>
public class AgentInfoFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentInfoFunctions> _logger;

    public AgentInfoFunctions(ApplicationDbContext context, ILogger<AgentInfoFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить статус агента
    /// Параметры: agentId (string)
    /// Возвращает: status (int)
    /// </summary>
    public async Task<int> GetAgentStatus(string agentId)
    {
        _logger.LogInformation("Getting status for agent {AgentId}", agentId);

        var agent = await _context.Agents
            .FirstOrDefaultAsync(a => a.NodeId == agentId);

        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", agentId);
            return -1;
        }

        return agent.Status;
    }

    /// <summary>
    /// Получить параметры агента
    /// Параметры: agentId (string)
    /// Возвращает: parameters (string) - JSON
    /// </summary>
    public async Task<string> GetAgentParameters(string agentId)
    {
        _logger.LogInformation("Getting parameters for agent {AgentId}", agentId);

        var agent = await _context.Agents
            .Include(a => a.Parameters)
            .FirstOrDefaultAsync(a => a.NodeId == agentId);

        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", agentId);
            return "{}";
        }

        var parameters = agent.Parameters.ToDictionary(
            p => p.Name,
            p => new { p.Value, p.Unit, p.Description }
        );

        return JsonConvert.SerializeObject(parameters);
    }

    /// <summary>
    /// Получить полную информацию об агенте
    /// Параметры: agentId (string)
    /// Возвращает: agentInfo (string) - JSON
    /// </summary>
    public async Task<string> GetAgentInfo(string agentId)
    {
        _logger.LogInformation("Getting full info for agent {AgentId}", agentId);

        var agent = await _context.Agents
            .Include(a => a.Parameters)
            .Include(a => a.TreeNode)
            .FirstOrDefaultAsync(a => a.NodeId == agentId);

        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", agentId);
            return "{}";
        }

        var info = new
        {
            agentId = agent.NodeId,
            name = agent.Name,
            type = agent.Type,
            agentType = agent.AgentTypeValue,
            profession = agent.Profession,
            okpd2Code = agent.Okpd2Code,
            okpd2Codes = JsonConvert.DeserializeObject<List<string>>(agent.Okpd2Codes),
            status = agent.Status,
            quantity = agent.Quantity,
            parameters = agent.Parameters.ToDictionary(
                p => p.Name,
                p => new { p.Value, p.Unit, p.Description }
            )
        };

        return JsonConvert.SerializeObject(info);
    }

    /// <summary>
    /// Обновить статус агента
    /// Параметры: agentId (string), status (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateAgentStatus(string agentId, int status)
    {
        _logger.LogInformation("Updating status for agent {AgentId} to {Status}", agentId, status);

        var agent = await _context.Agents
            .FirstOrDefaultAsync(a => a.NodeId == agentId);

        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", agentId);
            return "false";
        }

        agent.Status = status;
        await _context.SaveChangesAsync();

        return "true";
    }

    /// <summary>
    /// Получить список агентов по OKPD-2 коду
    /// Параметры: okpd2Code (string)
    /// Возвращает: agentIds (string) - JSON array
    /// </summary>
    public async Task<string> GetAgentsByOkpd2Code(string okpd2Code)
    {
        _logger.LogInformation("Getting agents with OKPD-2 code {Code}", okpd2Code);

        var agents = await _context.Agents
            .Where(a => a.Okpd2Code == okpd2Code ||
                       EF.Functions.JsonContains(a.Okpd2Codes, $"\"{okpd2Code}\""))
            .Select(a => a.NodeId)
            .ToListAsync();

        return JsonConvert.SerializeObject(agents);
    }
}
