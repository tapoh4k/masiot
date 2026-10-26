using Backend.Models;

namespace Backend.Services.Hosts;

public class HostAgentCommandDispatcher : IHostAgentCommandDispatcher
{
    private readonly StandardHostAgentCommandService _standard;
    private readonly SpecificHostAgentCommandService _specific;

    public HostAgentCommandDispatcher(
        StandardHostAgentCommandService standard,
        SpecificHostAgentCommandService specific)
    {
        _standard = standard;
        _specific = specific;
    }

    public Task SendAsync(
        AgentHost host,
        string command,
        IEnumerable<string> agentIds,
        Dictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default)
    {
        IHostAgentCommandService service = string.Equals(host.Type, HostTypes.Specific, StringComparison.OrdinalIgnoreCase)
            ? _specific
            : _standard;

        return service.SendCommandAsync(host, command, agentIds, payload, cancellationToken);
    }
}
