using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("timetable")]
public class Timetable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("order_number")]
    [MaxLength(255)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [Column("time_start")]
    public DateTime TimeStart { get; set; }

    [Required]
    [Column("time_end")]
    public DateTime TimeEnd { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; } = 0;

    [Column("location")]
    [MaxLength(255)]
    public string? Location { get; set; }

    [Column("status")]
    public int Status { get; set; } = 0;

    [Column("partner_id")]
    [MaxLength(255)]
    public string? PartnerId { get; set; }

    [Column("wf_id")]
    public int? WFId { get; set; }

    [Column("parameters", TypeName = "jsonb")]
    public string Parameters { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("WFId")]
    public WFList? WFList { get; set; }
}
