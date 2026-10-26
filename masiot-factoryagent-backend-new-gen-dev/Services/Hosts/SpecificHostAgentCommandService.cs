using Backend.Models;
using Backend.Services.RabbitMQ;

namespace Backend.Services.Hosts;

/// <summary>
/// Specific Host — точка расширения. Сейчас публикует тот же конверт с пометкой specific;
/// кастомная логика может быть добавлена без изменения Cluster/Single.
/// </summary>
public class SpecificHostAgentCommandService : IHostAgentCommandService
{
    private readonly IRabbitMQService _rabbitMq;
    private readonly ILogger<SpecificHostAgentCommandService> _logger;

    public SpecificHostAgentCommandService(IRabbitMQService rabbitMq, ILogger<SpecificHostAgentCommandService> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public string HostType => HostTypes.Specific;

    public Task SendCommandAsync(
        AgentHost host,
        string command,
        IEnumerable<string> agentIds,
        Dictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default)
    {
        var ids = agentIds.ToList();
        var extended = payload != null
            ? new Dictionary<string, object?>(payload)
            : new Dictionary<string, object?>();
        extended["specific"] = true;

        var message = new HostCommandMessage
        {
            Command = command,
            HostId = host.Id,
            HostType = host.Type,
            AgentIds = ids,
            Payload = extended,
            MessageType = "host.command.specific",
            ReceiverId = host.RoutingKey
        };

        _rabbitMq.SendMessage(message, host.RoutingKey, message.MessageType);
        _logger.LogInformation("Specific host command {Command} → {RoutingKey}", command, host.RoutingKey);
        return Task.CompletedTask;
    }
}
