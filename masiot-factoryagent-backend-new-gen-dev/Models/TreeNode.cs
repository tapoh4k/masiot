using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("tree_nodes")]
public class TreeNode
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("icon")]
    public string? Icon { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("parent_id")]
    public string? ParentId { get; set; }

    [Column("position")]
    public int? Position { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ParentId")]
    public TreeNode? Parent { get; set; }

    public ICollection<TreeNode> Children { get; set; } = new List<TreeNode>();

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
