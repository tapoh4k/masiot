using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Backend.DTOs;

public class WorkflowTemplateDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public JsonElement Nodes { get; set; }

    public JsonElement Edges { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateWorkflowTemplateDto
{
    [Required(ErrorMessage = "Название шаблона обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    public JsonElement? Nodes { get; set; }

    public JsonElement? Edges { get; set; }
}

public class UpdateWorkflowTemplateDto
{
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    public JsonElement? Nodes { get; set; }

    public JsonElement? Edges { get; set; }
}
