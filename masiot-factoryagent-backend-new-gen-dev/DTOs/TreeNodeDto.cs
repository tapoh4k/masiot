using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class TreeNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    public int? Position { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateTreeNodeDto
{
    [Required(ErrorMessage = "ID узла обязателен")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "ID должен быть от 1 до 255 символов")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Название узла обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Иконка не может превышать 100 символов")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Тип узла обязателен")]
    [StringLength(100, ErrorMessage = "Тип не может превышать 100 символов")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("parent_id")]
    [StringLength(255, ErrorMessage = "ID родителя не может превышать 255 символов")]
    public string? ParentId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Позиция должна быть неотрицательной")]
    public int? Position { get; set; }
}

public class UpdateTreeNodeDto
{
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string? Name { get; set; }

    [StringLength(100, ErrorMessage = "Иконка не может превышать 100 символов")]
    public string? Icon { get; set; }

    [StringLength(100, ErrorMessage = "Тип не может превышать 100 символов")]
    public string? Type { get; set; }

    [JsonPropertyName("parent_id")]
    [StringLength(255, ErrorMessage = "ID родителя не может превышать 255 символов")]
    public string? ParentId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Позиция должна быть неотрицательной")]
    public int? Position { get; set; }
}
