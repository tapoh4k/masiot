using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("blank_okpd2_mapping")]
public class BlankOkpd2Mapping
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("keywords")]
    public string Keywords { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("okpd2_code")]
    public string Okpd2Code { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
