using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("pending_messages")]
public class PendingMessage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("agent_id")]
    public int AgentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("kind")]
    public string Kind { get; set; } = "work";

    [Required]
    [MaxLength(255)]
    [Column("workflow_type")]
    public string WorkflowType { get; set; } = string.Empty;

    [Required]
    [Column("payload", TypeName = "jsonb")]
    public string Payload { get; set; } = "{}";

    [Required]
    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "draft"; // draft|sent|failed

    [Column("error")]
    public string? Error { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("AgentId")]
    public Agent Agent { get; set; } = null!;
}
