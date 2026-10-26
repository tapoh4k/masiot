using Backend.Models;

namespace Backend.Services.Hosts;

public interface IHostProvisioningService
{
    Task ProvisionRabbitMqAsync(AgentHost host, string plainPassword, CancellationToken cancellationToken = default);
    Task DeprovisionRabbitMqAsync(AgentHost host, CancellationToken cancellationToken = default);
    string GeneratePassword();
    string GenerateUsername(int hostId);
}
