using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class StartWorkflowDto
{
    [Required(ErrorMessage = "Тип workflow обязателен")]
    [StringLength(255, ErrorMessage = "Тип workflow не может превышать 255 символов")]
    [JsonPropertyName("wf_type")]
    public string WFType { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID агента обязателен")]
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Параметры обязательны")]
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [StringLength(255, ErrorMessage = "Инициатор не может превышать 255 символов")]
    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }
}

public class ContinueWorkflowDto
{
    [Range(0, int.MaxValue, ErrorMessage = "Шаг должен быть неотрицательным")]
    [JsonPropertyName("from_step")]
    public int FromStep { get; set; }

    [JsonPropertyName("additional_parameters")]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

public class UpdateStepDto
{
    [JsonPropertyName("step")]
    public int Step { get; set; }
}

public class UpdateStatusDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
}

public class WFListDto
{
    public int Id { get; set; }
    public int Step { get; set; }

    [JsonPropertyName("time_start")]
    public DateTime TimeStart { get; set; }

    public int Status { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("wf_type")]
    public string WFType { get; set; } = string.Empty;

    [JsonPropertyName("wf_functions")]
    public List<object> WFFunctions { get; set; } = new();

    public string? Initiator { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
