using Backend.Services.RabbitMQ;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для отправки сообщений через RabbitMQ
/// Категория: RabbitMQ
/// </summary>
public class RabbitMQFunctions
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<RabbitMQFunctions> _logger;

    public RabbitMQFunctions(IRabbitMQService rabbitMQService, ILogger<RabbitMQFunctions> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    /// <summary>
    /// Отправить сообщение агенту
    /// Параметры: agentId (string), messageType (string), payload (string - JSON)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string SendToAgent(string agentId, string messageType, string payload)
    {
        _logger.LogInformation("Sending message to agent {AgentId}, type {MessageType}", agentId, messageType);

        var message = JsonConvert.DeserializeObject<object>(payload);
        _rabbitMQService.SendToAgent(message!, agentId, messageType);

        return "sent";
    }

    /// <summary>
    /// Отправить сообщение всем агентам с OKPD-2 кодом
    /// Параметры: okpd2Code (string), messageType (string), payload (string - JSON)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string SendToOkpd2Code(string okpd2Code, string messageType, string payload)
    {
        _logger.LogInformation("Broadcasting message to OKPD-2 code {Code}, type {MessageType}", okpd2Code, messageType);

        var message = JsonConvert.DeserializeObject<object>(payload);
        _rabbitMQService.SendToOkpd2Code(message!, okpd2Code, messageType);

        return "sent";
    }

    /// <summary>
    /// Отправить сообщение нескольким OKPD-2 кодам
    /// Параметры: okpd2Codes (string - JSON array), messageType (string), payload (string - JSON)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string BroadcastToOkpd2Codes(string okpd2Codes, string messageType, string payload)
    {
        _logger.LogInformation("Broadcasting message to multiple OKPD-2 codes, type {MessageType}", messageType);

        var codes = JsonConvert.DeserializeObject<List<string>>(okpd2Codes);
        var message = JsonConvert.DeserializeObject<object>(payload);

        if (codes != null && message != null)
        {
            _rabbitMQService.BroadcastToOkpd2Codes(message, codes, messageType);
        }

        return "sent";
    }

    /// <summary>
    /// Отправить сообщение с произвольным routing key
    /// Параметры: routingKey (string), messageType (string), payload (string - JSON)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string SendMessage(string routingKey, string messageType, string payload)
    {
        _logger.LogInformation("Sending message to routing key {RoutingKey}, type {MessageType}", routingKey, messageType);

        var message = JsonConvert.DeserializeObject<object>(payload);
        _rabbitMQService.SendMessage(message!, routingKey, messageType);

        return "sent";
    }
}
