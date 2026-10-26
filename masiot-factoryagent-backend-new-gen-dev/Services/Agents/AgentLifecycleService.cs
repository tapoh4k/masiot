using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services.Hosts;
using Backend.Services.RabbitMQ;

namespace Backend.Services.Agents;

public class AgentLifecycleService : IAgentLifecycleService
{
    private readonly ApplicationDbContext _context;
    private readonly IRabbitMQService _rabbitMq;
    private readonly IHostAgentCommandDispatcher _hostCommands;
    private readonly ILogger<AgentLifecycleService> _logger;

    public AgentLifecycleService(
        ApplicationDbContext context,
        IRabbitMQService rabbitMq,
        IHostAgentCommandDispatcher hostCommands,
        ILogger<AgentLifecycleService> logger)
    {
        _context = context;
        _rabbitMq = rabbitMq;
        _hostCommands = hostCommands;
        _logger = logger;
    }

    public async Task ValidateHostCapacityAsync(int hostId, CancellationToken cancellationToken = default)
    {
        var host = await _context.Hosts.FirstOrDefaultAsync(h => h.Id == hostId, cancellationToken)
            ?? throw new InvalidOperationException($"Host {hostId} не найден");

        var count = await _context.Agents.CountAsync(a => a.HostId == hostId, cancellationToken);
        if (count >= host.MaxAgents)
        {
            throw new InvalidOperationException(
                $"Host '{host.Name}' ({host.Type}) достиг лимита агентов ({host.MaxAgents})");
        }
    }

    public async Task ProvisionAgentAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        var agentId = AgentRoutingHelper.BuildAgentId(agent.Id);
        agent.Queue = agentId;

        var routingKeys = AgentRoutingHelper.GetAgentRoutingKeys(agent);
        await _rabbitMq.EnsureQueueWithBindingsAsync(agentId, routingKeys, cancellationToken);

        var parameters = agent.Parameters?
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .ToDictionary(p => p.Name, p => (object?)(p.Value ?? string.Empty))
            ?? new Dictionary<string, object?>();

        var configMessage = new
        {
            parameter = new
            {
                agentId,
                status = agent.Status,
                profession = agent.Profession ?? string.Empty,
                quantity = agent.Quantity ?? 1,
                parameters
            },
            messageType = "3-6-1-2",
            senderId = "fabric",
            receiverId = agentId
        };

        _rabbitMq.SendMessage(configMessage, agentId, "3-6-1-2");

        var host = await _context.Hosts.FirstOrDefaultAsync(h => h.Id == agent.HostId, cancellationToken);
        if (host != null)
        {
            await _hostCommands.SendAsync(
                host,
                HostAgentCommandTypes.CreateAgent,
                new[] { agentId },
                new Dictionary<string, object?>
                {
                    ["name"] = agent.Name,
                    ["profession"] = agent.Profession,
                    ["quantity"] = agent.Quantity,
                    ["status"] = agent.Status,
                    ["okpd2_code"] = agent.Okpd2Code
                },
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Agent {AgentId} provisioned on host {HostId}", agentId, agent.HostId);
    }

    public async Task DeprovisionAgentAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        var agentId = AgentRoutingHelper.BuildAgentId(agent.Id);

        var host = await _context.Hosts.FirstOrDefaultAsync(h => h.Id == agent.HostId, cancellationToken);
        if (host != null)
        {
            try
            {
                await _hostCommands.SendAsync(
                    host,
                    HostAgentCommandTypes.DeleteAgent,
                    new[] { agentId },
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify host about agent delete {AgentId}", agentId);
            }
        }

        try
        {
            await _rabbitMq.DeleteQueueAsync(agentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete agent queue {AgentId}", agentId);
        }
    }
}
