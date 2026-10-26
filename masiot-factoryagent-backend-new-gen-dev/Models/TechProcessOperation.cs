using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("tech_process_operations")]
public class TechProcessOperation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("tech_process_id")]
    public int TechProcessId { get; set; }

    [Column("op_no")]
    public int? OpNo { get; set; }

    [MaxLength(255)]
    [Column("op_type")]
    public string? OpType { get; set; }

    [MaxLength(255)]
    [Column("blank_code")]
    public string? BlankCode { get; set; }

    [Column("setup_time_min")]
    public double? SetupTimeMin { get; set; }

    [Column("process_time_min")]
    public double? ProcessTimeMin { get; set; }

    [Column("service_time_min")]
    public double? ServiceTimeMin { get; set; }

    [MaxLength(255)]
    [Column("equipment_code")]
    public string? EquipmentCode { get; set; }

    [Column("tool_codes")]
    public string? ToolCodes { get; set; }

    [MaxLength(255)]
    [Column("profession_code")]
    public string? ProfessionCode { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("extra")]
    public string? Extra { get; set; }

    [Column("position")]
    public int Position { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("TechProcessId")]
    public TechProcess TechProcess { get; set; } = null!;
}
