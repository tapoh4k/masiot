using Backend.Models;

namespace Backend.Services.Hosts;

public interface IHostAgentCommandService
{
    string HostType { get; }

    Task SendCommandAsync(
        AgentHost host,
        string command,
        IEnumerable<string> agentIds,
        Dictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default);
}

public interface IHostAgentCommandDispatcher
{
    Task SendAsync(
        AgentHost host,
        string command,
        IEnumerable<string> agentIds,
        Dictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default);
}
