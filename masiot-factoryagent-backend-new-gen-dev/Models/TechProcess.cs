using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("tech_processes")]
public class TechProcess
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("name")]
    public string? Name { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("source_kind")]
    public string SourceKind { get; set; } = "xlsx";

    [MaxLength(255)]
    [Column("source_file_name")]
    public string? SourceFileName { get; set; }

    [MaxLength(255)]
    [Column("source_sheet_name")]
    public string? SourceSheetName { get; set; }

    [MaxLength(100)]
    [Column("source_checksum")]
    public string? SourceChecksum { get; set; }

    [Column("imported_at")]
    public DateTime? ImportedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TechProcessOperation> Operations { get; set; } = new List<TechProcessOperation>();
}
