using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class AgentDto
{
    public int Id { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("host_id")]
    public int HostId { get; set; }

    [JsonPropertyName("host_name")]
    public string? HostName { get; set; }

    public string? Queue { get; set; }
    public string? Name { get; set; }
    public string Type { get; set; } = "other";
    public string? Profession { get; set; }
    public int Status { get; set; } = 0;
    public int? Quantity { get; set; }

    [JsonPropertyName("queue_number")]
    public long? QueueNumber { get; set; }

    // OKPD-2 classification fields
    [JsonPropertyName("okpd2_code")]
    public string? Okpd2Code { get; set; }

    [JsonPropertyName("agent_type")]
    public string AgentTypeValue { get; set; } = "resource";

    public List<AgentParameterDto> Parameters { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Workflow status fields
    [JsonPropertyName("workflow_applied")]
    public bool WorkflowApplied { get; set; } = false;

    [JsonPropertyName("workflow_status")]
    public string WorkflowStatus { get; set; } = "not_ready";

    [JsonPropertyName("applied_workflows")]
    public List<string> AppliedWorkflows { get; set; } = new();

    [JsonPropertyName("tech_process_id")]
    public int? TechProcessId { get; set; }

    // Latest draft message to start planning (prepared but not sent)
    [JsonPropertyName("pending_message")]
    public PendingMessageDto? PendingMessage { get; set; }
}

public class CreateAgentDto
{
    [Required(ErrorMessage = "NodeId обязателен")]
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "HostId обязателен")]
    [JsonPropertyName("host_id")]
    [Range(1, int.MaxValue, ErrorMessage = "HostId обязателен")]
    public int HostId { get; set; }

    [Required(ErrorMessage = "Имя агента обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Имя должно быть от 1 до 255 символов")]
    public string Name { get; set; } = string.Empty;

    public string? Queue { get; set; }

    [Required(ErrorMessage = "Тип агента обязателен")]
    [RegularExpression("^(tech_process|equipment|instrument|personnel|blank)$",
        ErrorMessage = "Недопустимый тип агента")]
    public string Type { get; set; } = "other";

    [StringLength(500, ErrorMessage = "Профессия не может превышать 500 символов")]
    public string? Profession { get; set; }

    public int Status { get; set; } = 0;

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("queue_number")]
    [Range(1, long.MaxValue, ErrorMessage = "Номер в очереди должен быть больше 0")]
    public long QueueNumber { get; set; } = 1;

    [JsonPropertyName("okpd2_code")]
    [StringLength(255, ErrorMessage = "OKPD-2 код не может превышать 255 символов")]
    public string? Okpd2Code { get; set; }

    public List<CreateAgentParameterDto>? Parameters { get; set; }
}

public class UpdateAgentDto
{
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("host_id")]
    public int? HostId { get; set; }

    [StringLength(255, MinimumLength = 1, ErrorMessage = "Имя должно быть от 1 до 255 символов")]
    public string? Name { get; set; }

    public string? Queue { get; set; }

    public string? Type { get; set; }

    [StringLength(500, ErrorMessage = "Профессия не может превышать 500 символов")]
    public string? Profession { get; set; }

    public int? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public int? Quantity { get; set; }

    [JsonPropertyName("queue_number")]
    [Range(1, long.MaxValue, ErrorMessage = "Номер в очереди должен быть больше 0")]
    public long? QueueNumber { get; set; }

    // OKPD-2 classification fields
    [JsonPropertyName("okpd2_code")]
    [StringLength(255, ErrorMessage = "OKPD-2 код не может превышать 255 символов")]
    public string? Okpd2Code { get; set; }

    [JsonPropertyName("agent_type")]
    [StringLength(50, ErrorMessage = "Тип агента не может превышать 50 символов")]
    public string? AgentTypeValue { get; set; }

    // Workflow status fields
    [JsonPropertyName("workflow_applied")]
    public bool? WorkflowApplied { get; set; }

    [JsonPropertyName("workflow_status")]
    [StringLength(50, ErrorMessage = "Статус workflow не может превышать 50 символов")]
    public string? WorkflowStatus { get; set; }

    [JsonPropertyName("applied_workflows")]
    public List<string>? AppliedWorkflows { get; set; }

    public List<UpdateAgentParameterDto>? Parameters { get; set; }
}

public class AgentParameterDto
{
    public int Id { get; set; }

    [JsonPropertyName("agent_id")]
    public int AgentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateAgentParameterDto
{
    [Required(ErrorMessage = "Имя параметра обязательно")]
    [StringLength(255, ErrorMessage = "Имя параметра не может превышать 255 символов")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Единица измерения не может превышать 50 символов")]
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [StringLength(500, ErrorMessage = "Значение не может превышать 500 символов")]
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class UpdateAgentParameterDto
{
    [StringLength(255, ErrorMessage = "Имя параметра не может превышать 255 символов")]
    public string? Name { get; set; }

    [StringLength(50, ErrorMessage = "Единица измерения не может превышать 50 символов")]
    public string? Unit { get; set; }

    [StringLength(500, ErrorMessage = "Значение не может превышать 500 символов")]
    public string? Value { get; set; }

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }
}
