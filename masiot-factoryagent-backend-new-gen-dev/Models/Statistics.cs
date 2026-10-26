using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("statistics")]
public class Statistics
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("agents_count")]
    public int AgentsCount { get; set; }

    [Column("equipment_count")]
    public int EquipmentCount { get; set; }

    [Column("processes_count")]
    public int ProcessesCount { get; set; }

    [Column("instructions_count")]
    public int InstructionsCount { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
