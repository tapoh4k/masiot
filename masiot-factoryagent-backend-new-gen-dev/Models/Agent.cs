using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public enum AgentType
{
    Resource,
    TechProcess
}

[Table("agents")]
public class Agent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("queue")]
    public string? Queue { get; set; }

    [Column("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [Column("host_id")]
    public int HostId { get; set; }

    [ForeignKey("HostId")]
    public AgentHost Host { get; set; } = null!;

    [MaxLength(255)]
    [Column("name")]
    public string? Name { get; set; }

    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = "other";

    // Agent type: resource or tech_process
    [Column("agent_type")]
    [MaxLength(50)]
    public string AgentTypeValue { get; set; } = "resource";

    [NotMapped]
    public AgentType AgentType
    {
        get => Enum.TryParse<AgentType>(AgentTypeValue, true, out var result) ? result : AgentType.Resource;
        set => AgentTypeValue = value.ToString().ToLower();
    }

    [MaxLength(255)]
    [Column("profession")]
    public string? Profession { get; set; }

    // Primary OKPD-2 code
    [MaxLength(255)]
    [Column("okpd2_code")]
    public string? Okpd2Code { get; set; }

    // Multiple OKPD-2 codes (JSONB array)
    [Column("okpd2_codes", TypeName = "jsonb")]
    public string Okpd2Codes { get; set; } = "[]";

    [Column("status")]
    public int Status { get; set; } = 0;

    [Column("quantity")]
    public int? Quantity { get; set; }

    [Column("queue_number")]
    public long? QueueNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Workflow status fields
    [Column("workflow_applied")]
    public bool WorkflowApplied { get; set; } = false;

    [Column("workflow_status")]
    [MaxLength(50)]
    public string WorkflowStatus { get; set; } = "not_ready";

    [Column("applied_workflows", TypeName = "jsonb")]
    public string AppliedWorkflows { get; set; } = "[]";

    // Navigation properties
    [ForeignKey("NodeId")]
    public TreeNode TreeNode { get; set; } = null!;

    public ICollection<AgentParameter> Parameters { get; set; } = new List<AgentParameter>();

    // Tech process attachment (one agent -> one tech process)
    [Column("tech_process_id")]
    public int? TechProcessId { get; set; }

    [ForeignKey("TechProcessId")]
    public TechProcess? TechProcess { get; set; }

    public ICollection<PendingMessage> PendingMessages { get; set; } = new List<PendingMessage>();
}
