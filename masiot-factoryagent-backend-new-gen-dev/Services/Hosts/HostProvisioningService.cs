using System.Security.Cryptography;
using Backend.Models;
using Backend.Services.RabbitMQ;

namespace Backend.Services.Hosts;

public class HostProvisioningService : IHostProvisioningService
{
    private readonly IRabbitMQService _rabbitMq;
    private readonly IRabbitMQManagementService _management;
    private readonly ILogger<HostProvisioningService> _logger;

    public HostProvisioningService(
        IRabbitMQService rabbitMq,
        IRabbitMQManagementService management,
        ILogger<HostProvisioningService> logger)
    {
        _rabbitMq = rabbitMq;
        _management = management;
        _logger = logger;
    }

    public string GenerateUsername(int hostId) => $"host_{hostId}";

    public string GeneratePassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace("+", "A")
            .Replace("/", "B")
            .TrimEnd('=');
    }

    public async Task ProvisionRabbitMqAsync(AgentHost host, string plainPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host.RmqUsername))
            host.RmqUsername = GenerateUsername(host.Id);

        host.QueueName = AgentRoutingHelper.BuildHostQueueName(host.Id);
        host.RoutingKey = AgentRoutingHelper.BuildHostRoutingKey(host.Id);

        try
        {
            await _management.EnsureUserAsync(host.RmqUsername, plainPassword, cancellationToken);
        }
        catch (Exception ex)
        {
            // Dev без Management API — очередь всё равно создаём
            _logger.LogWarning(ex, "RabbitMQ Management user provision failed for host {HostId}; continuing with queue bind", host.Id);
        }

        await _rabbitMq.EnsureQueueWithBindingsAsync(host.QueueName, new[] { host.RoutingKey }, cancellationToken);
        _logger.LogInformation("Host {HostId} provisioned queue {Queue} key {Key}", host.Id, host.QueueName, host.RoutingKey);
    }

    public async Task DeprovisionRabbitMqAsync(AgentHost host, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(host.QueueName))
        {
            try
            {
                await _rabbitMq.DeleteQueueAsync(host.QueueName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete host queue {Queue}", host.QueueName);
            }
        }

        if (!string.IsNullOrWhiteSpace(host.RmqUsername))
        {
            try
            {
                await _management.DeleteUserAsync(host.RmqUsername, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete RMQ user {User}", host.RmqUsername);
            }
        }
    }
}
