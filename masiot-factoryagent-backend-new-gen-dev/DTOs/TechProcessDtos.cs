using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class TechProcessDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }

    [JsonPropertyName("sheet_name")]
    public string? SheetName { get; set; }

    public List<TechProcessOperationDto> Operations { get; set; } = new();
}

public class TechProcessOperationDto
{
    [JsonPropertyName("op_no")]
    public int? OpNo { get; set; }

    [JsonPropertyName("op_type")]
    public string? OpType { get; set; }

    [JsonPropertyName("blank_code")]
    public string? BlankCode { get; set; }

    [JsonPropertyName("setup_time_min")]
    public double? SetupTimeMin { get; set; }

    [JsonPropertyName("process_time_min")]
    public double? ProcessTimeMin { get; set; }

    [JsonPropertyName("service_time_min")]
    public double? ServiceTimeMin { get; set; }

    [JsonPropertyName("equipment_code")]
    public string? EquipmentCode { get; set; }

    [JsonPropertyName("tool_codes")]
    public List<string> ToolCodes { get; set; } = new();

    [JsonPropertyName("profession_code")]
    public string? ProfessionCode { get; set; }

    public string? Description { get; set; }
    public string? Extra { get; set; }
}

public class PendingMessageDto
{
    public int Id { get; set; }

    [JsonPropertyName("agent_id")]
    public int AgentId { get; set; }

    public string Kind { get; set; } = "work";

    [JsonPropertyName("workflow_type")]
    public string WorkflowType { get; set; } = string.Empty;

    public string Status { get; set; } = "draft";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("sent_at")]
    public DateTime? SentAt { get; set; }

    public object Payload { get; set; } = new();
}
