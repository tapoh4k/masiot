namespace Backend.Services.RabbitMQ;

public class RabbitMQHostedService : IHostedService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<RabbitMQHostedService> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    public RabbitMQHostedService(IRabbitMQService rabbitMQService, ILogger<RabbitMQHostedService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Hosted Service is starting");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start listening to RabbitMQ messages in the background
        _listenerTask = Task.Run(async () =>
        {
            try
            {
                // Listen to all messages with wildcard routing key
                await _rabbitMQService.StartListeningAsync(
                    queueName: "backend_queue",
                    routingKeys: new List<string> { "#" }, // Listen to all messages
                    cancellationToken: _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RabbitMQ listener task");
            }
        }, _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Hosted Service is stopping");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_listenerTask != null)
        {
            await Task.WhenAny(_listenerTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        _cancellationTokenSource?.Dispose();
    }
}
