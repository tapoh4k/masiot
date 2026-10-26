namespace Backend.Services.Hosts;

/// <summary>Команды Host ↔ Agent (ТЗ §8). Runtime Host обрабатывает их отдельно.</summary>
public static class HostAgentCommandTypes
{
    public const string CreateAgent = "host.create_agent";
    public const string CreateAgents = "host.create_agents";
    public const string StartAgent = "host.start_agent";
    public const string StartAgents = "host.start_agents";
    public const string StartAllAgents = "host.start_all_agents";
    public const string StopAgent = "host.stop_agent";
    public const string StopAgents = "host.stop_agents";
    public const string StopAllAgents = "host.stop_all_agents";
    public const string DeleteAgent = "host.delete_agent";
    public const string DeleteAgents = "host.delete_agents";
    public const string DeleteAllAgents = "host.delete_all_agents";
    public const string MoveAgent = "host.move_agent";
    public const string MoveAgents = "host.move_agents";
}

public class HostCommandMessage
{
    public string Command { get; set; } = string.Empty;
    public int HostId { get; set; }
    public string HostType { get; set; } = string.Empty;
    public List<string> AgentIds { get; set; } = new();
    public Dictionary<string, object?> Payload { get; set; } = new();
    public string MessageType { get; set; } = "host.command";
    public string SenderId { get; set; } = "fabric";
    public string ReceiverId { get; set; } = string.Empty;
}
