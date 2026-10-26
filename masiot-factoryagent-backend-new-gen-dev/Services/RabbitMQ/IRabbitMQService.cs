namespace Backend.Services.RabbitMQ;

public interface IRabbitMQService
{
    Task SendMessageAsync(string queueName, object message, string routingKey);
    void SendMessage(object message, string routingKey, string messageType);
    Task StartListeningAsync(string queueName, List<string> routingKeys, CancellationToken cancellationToken);

    Task EnsureQueueWithBindingsAsync(string queueName, IEnumerable<string> routingKeys, CancellationToken cancellationToken = default);
    Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default);

    // Dual routing support for agents
    void SendToAgent(object message, string agentId, string messageType);
    void SendToOkpd2Code(object message, string okpd2Code, string messageType);
    void BroadcastToOkpd2Codes(object message, List<string> okpd2Codes, string messageType);
    List<string> GetAgentRoutingKeys(string agentId, List<string> okpd2Codes);
}
