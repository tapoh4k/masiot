using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public static class HostTypes
{
    public const string Cluster = "cluster";
    public const string Single = "single";
    public const string Specific = "specific";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Cluster, Single, Specific
    };
}

public static class HostDeployTargets
{
    public const string Docker = "docker";
    public const string Machine = "machine";
    public const string Custom = "custom";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Docker, Machine, Custom
    };
}

[Table("hosts")]
public class AgentHost
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>cluster | single | specific</summary>
    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = HostTypes.Cluster;

    /// <summary>online | offline | unknown</summary>
    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "offline";

    /// <summary>Максимум агентов. Single=1, Cluster/Specific — N.</summary>
    [Column("max_agents")]
    public int MaxAgents { get; set; } = 100;

    /// <summary>docker | machine | custom</summary>
    [MaxLength(50)]
    [Column("deploy_target")]
    public string DeployTarget { get; set; } = HostDeployTargets.Docker;

    [MaxLength(255)]
    [Column("rmq_username")]
    public string? RmqUsername { get; set; }

    [Column("rmq_password_enc")]
    public string? RmqPasswordEnc { get; set; }

    [MaxLength(255)]
    [Column("queue_name")]
    public string QueueName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("routing_key")]
    public string RoutingKey { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
