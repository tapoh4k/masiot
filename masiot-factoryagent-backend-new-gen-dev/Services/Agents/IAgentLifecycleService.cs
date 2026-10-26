using Backend.Models;

namespace Backend.Services.Agents;

public interface IAgentLifecycleService
{
    Task ValidateHostCapacityAsync(int hostId, CancellationToken cancellationToken = default);
    Task ProvisionAgentAsync(Agent agent, CancellationToken cancellationToken = default);
    Task DeprovisionAgentAsync(Agent agent, CancellationToken cancellationToken = default);
}
