using Backend.Models;
using Backend.Services.RabbitMQ;

namespace Backend.Services.Hosts;

/// <summary>Cluster / Single — единый формат команд через RabbitMQ.</summary>
public class StandardHostAgentCommandService : IHostAgentCommandService
{
    private readonly IRabbitMQService _rabbitMq;
    private readonly ILogger<StandardHostAgentCommandService> _logger;

    public StandardHostAgentCommandService(IRabbitMQService rabbitMq, ILogger<StandardHostAgentCommandService> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public string HostType => $"{HostTypes.Cluster}|{HostTypes.Single}";

    public Task SendCommandAsync(
        AgentHost host,
        string command,
        IEnumerable<string> agentIds,
        Dictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default)
    {
        var ids = agentIds.ToList();
        var message = new HostCommandMessage
        {
            Command = command,
            HostId = host.Id,
            HostType = host.Type,
            AgentIds = ids,
            Payload = payload ?? new Dictionary<string, object?>(),
            ReceiverId = host.RoutingKey
        };

        _rabbitMq.SendMessage(message, host.RoutingKey, message.MessageType);
        _logger.LogInformation("Host command {Command} → {RoutingKey} agents={Count}",
            command, host.RoutingKey, ids.Count);
        return Task.CompletedTask;
    }
}
