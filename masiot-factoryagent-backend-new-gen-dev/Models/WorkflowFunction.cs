using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("workflow_functions")]
public class WorkflowFunction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("function_class")]
    [MaxLength(500)]
    public string FunctionClass { get; set; } = "";

    [Required]
    [Column("function_name")]
    [MaxLength(255)]
    public string FunctionName { get; set; } = "";

    [Column("params", TypeName = "jsonb")]
    public string Params { get; set; } = "[]";

    [Column("returns")]
    [MaxLength(255)]
    public string? Returns { get; set; }

    [Column("icon")]
    [MaxLength(10)]
    public string? Icon { get; set; }

    [Column("type")]
    [MaxLength(50)]
    public string Type { get; set; } = "function";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
