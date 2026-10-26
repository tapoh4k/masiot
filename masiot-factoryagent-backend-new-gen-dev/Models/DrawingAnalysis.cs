using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("drawing_analyses")]
public class DrawingAnalysis
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("content_type")]
    public string? ContentType { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "completed";

    [Column("part_name")]
    public string? PartName { get; set; }

    [Column("drawing_number")]
    [MaxLength(128)]
    public string? DrawingNumber { get; set; }

    [Column("material")]
    [MaxLength(256)]
    public string? Material { get; set; }

    [Column("total_labor_hours")]
    public decimal? TotalLaborHours { get; set; }

    [Column("confidence")]
    [MaxLength(32)]
    public string? Confidence { get; set; }

    /// <summary>Full structured JSON from the model (operations, GOST refs, assumptions).</summary>
    [Column("result_json", TypeName = "jsonb")]
    public string ResultJson { get; set; } = "{}";

    [Column("model_name")]
    [MaxLength(128)]
    public string? ModelName { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
