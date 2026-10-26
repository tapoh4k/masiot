using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Backend.Services;

namespace Backend.Services.RabbitMQ;

public class RabbitMQService : IRabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQMessageEncryptionService _encryptionService;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQService(ILogger<RabbitMQService> logger, IConfiguration configuration, IServiceProvider serviceProvider, IRabbitMQMessageEncryptionService encryptionService)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _encryptionService = encryptionService;
    }

    private void EnsureConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare("topic_logs", "topic", durable: true);

            _logger.LogInformation("RabbitMQ connection established");
        }
    }

    public void SendMessage(object message, string routingKey, string messageType)
    {
        try
        {
            EnsureConnection();

            var json = JsonConvert.SerializeObject(message);
            var encryptedJson = _encryptionService.EncryptPayload(json);
            var body = Encoding.UTF8.GetBytes(encryptedJson);

            var properties = _channel!.CreateBasicProperties();
            properties.Type = messageType;
            properties.ContentType = "application/json";
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "topic_logs",
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Message sent to {RoutingKey} with type {MessageType}", routingKey, messageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to RabbitMQ");
            throw;
        }
    }

    public async Task SendMessageAsync(string queueName, object message, string routingKey)
    {
        await Task.Run(() =>
        {
            EnsureConnection();
            var channel = _channel!;

            channel.ExchangeDeclare(exchange: "topic_logs", type: ExchangeType.Topic);
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queue: queueName, exchange: "topic_logs", routingKey: routingKey);

            var jsonMessage = JsonConvert.SerializeObject(message);
            var encryptedJson = _encryptionService.EncryptPayload(jsonMessage);
            var body = Encoding.UTF8.GetBytes(encryptedJson);

            channel.BasicPublish(exchange: "topic_logs", routingKey: routingKey, basicProperties: null, body: body);
        });
    }

    public async Task EnsureQueueWithBindingsAsync(string queueName, IEnumerable<string> routingKeys, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            EnsureConnection();
            var channel = _channel!;
            channel.ExchangeDeclare("topic_logs", ExchangeType.Topic, durable: true);
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            foreach (var key in routingKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct())
            {
                channel.QueueBind(queue: queueName, exchange: "topic_logs", routingKey: key);
                _logger.LogInformation("Queue {Queue} bound to {RoutingKey}", queueName, key);
            }
        }, cancellationToken);
    }

    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            EnsureConnection();
            _channel!.QueueDelete(queue: queueName, ifUnused: false, ifEmpty: false);
            _logger.LogInformation("Queue {Queue} deleted", queueName);
        }, cancellationToken);
    }

    // Send message directly to a specific agent (AgentId = agent.{id} or bare numeric → normalize)
    public void SendToAgent(object message, string agentId, string messageType)
    {
        var routingKey = agentId.StartsWith("agent.", StringComparison.OrdinalIgnoreCase)
            ? agentId
            : $"agent.{agentId}";
        SendMessage(message, routingKey, messageType);
    }

    // Profession / OKPD codes: точки заменяем на _
    public void SendToOkpd2Code(object message, string okpd2Code, string messageType)
    {
        var routingKey = AgentRoutingHelper.NormalizeRoutingCode(okpd2Code);
        SendMessage(message, routingKey, messageType);
    }

    public void BroadcastToOkpd2Codes(object message, List<string> okpd2Codes, string messageType)
    {
        foreach (var code in okpd2Codes)
        {
            SendToOkpd2Code(message, code, messageType);
        }
    }

    public List<string> GetAgentRoutingKeys(string agentId, List<string> okpd2Codes)
    {
        var canonicalId = agentId.StartsWith("agent.", StringComparison.OrdinalIgnoreCase)
            ? agentId
            : $"agent.{agentId}";

        var routingKeys = new List<string> { canonicalId };
        foreach (var code in okpd2Codes)
        {
            var normalized = AgentRoutingHelper.NormalizeRoutingCode(code);
            if (!string.IsNullOrWhiteSpace(normalized))
                routingKeys.Add(normalized);
        }

        return routingKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task StartListeningAsync(string queueName, List<string> routingKeys, CancellationToken cancellationToken)
    {
        try
        {
            EnsureConnection();

            _channel!.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            foreach (var key in routingKeys)
            {
                _channel.QueueBind(queueName, "topic_logs", key);
                _logger.LogInformation("Queue {QueueName} bound to routing key {RoutingKey}", queueName, key);
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var rawMessage = Encoding.UTF8.GetString(body);
                var message = _encryptionService.DecryptPayload(rawMessage);
                var messageType = ea.BasicProperties.Type;

                _logger.LogInformation("Received message of type {MessageType}", messageType);

                try
                {
                    // Create a scope to resolve scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var messageProcessingService = scope.ServiceProvider.GetRequiredService<IMessageProcessingService>();

                    await messageProcessingService.ProcessMessageAsync(message, messageType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message of type {MessageType}", messageType);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            _logger.LogInformation("Started listening on queue {QueueName}", queueName);

            // Keep listening until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ listener stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ listener");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
