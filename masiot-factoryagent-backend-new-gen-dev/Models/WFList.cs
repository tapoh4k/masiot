using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("wf_list")]
public class WFList
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("step")]
    public int Step { get; set; } = 0;

    [Column("time_start")]
    public DateTime TimeStart { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public int Status { get; set; } = 0;

    [Required]
    [Column("parameters", TypeName = "jsonb")]
    public string Parameters { get; set; } = "{}";

    [Required]
    [Column("agent_id")]
    [MaxLength(255)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    [Column("wf_type")]
    [MaxLength(255)]
    public string WFType { get; set; } = string.Empty;

    [Required]
    [Column("wf_functions", TypeName = "jsonb")]
    public string WFFunctions { get; set; } = "[]";

    [Column("initiator")]
    [MaxLength(255)]
    public string? Initiator { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
