using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class DrawingAnalysisDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "completed";

    [JsonPropertyName("part_name")]
    public string? PartName { get; set; }

    [JsonPropertyName("drawing_number")]
    public string? DrawingNumber { get; set; }

    [JsonPropertyName("material")]
    public string? Material { get; set; }

    [JsonPropertyName("total_labor_hours")]
    public decimal? TotalLaborHours { get; set; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    [JsonPropertyName("result")]
    public DrawingLaborEstimateDto? Result { get; set; }

    [JsonPropertyName("result_json")]
    public string? ResultJson { get; set; }

    [JsonPropertyName("model_name")]
    public string? ModelName { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class DrawingLaborEstimateDto
{
    [JsonPropertyName("part_name")]
    public string? PartName { get; set; }

    [JsonPropertyName("drawing_number")]
    public string? DrawingNumber { get; set; }

    [JsonPropertyName("material")]
    public string? Material { get; set; }

    [JsonPropertyName("blank_type")]
    public string? BlankType { get; set; }

    [JsonPropertyName("overall_dimensions")]
    public string? OverallDimensions { get; set; }

    [JsonPropertyName("operations")]
    public List<DrawingOperationEstimateDto> Operations { get; set; } = new();

    [JsonPropertyName("total_setup_min")]
    public decimal? TotalSetupMin { get; set; }

    [JsonPropertyName("total_piece_min")]
    public decimal? TotalPieceMin { get; set; }

    [JsonPropertyName("total_labor_hours")]
    public decimal? TotalLaborHours { get; set; }

    [JsonPropertyName("batch_size_assumed")]
    public int? BatchSizeAssumed { get; set; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    [JsonPropertyName("gost_references")]
    public List<string> GostReferences { get; set; } = new();

    [JsonPropertyName("assumptions")]
    public List<string> Assumptions { get; set; } = new();

    [JsonPropertyName("risks")]
    public List<string> Risks { get; set; } = new();

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public class DrawingOperationEstimateDto
{
    [JsonPropertyName("op_no")]
    public int? OpNo { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("equipment")]
    public string? Equipment { get; set; }

    [JsonPropertyName("profession")]
    public string? Profession { get; set; }

    [JsonPropertyName("setup_min")]
    public decimal? SetupMin { get; set; }

    [JsonPropertyName("piece_min")]
    public decimal? PieceMin { get; set; }

    [JsonPropertyName("labor_hours")]
    public decimal? LaborHours { get; set; }

    [JsonPropertyName("gost_refs")]
    public List<string> GostRefs { get; set; } = new();

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class AnalyzeDrawingResponseDto
{
    [JsonPropertyName("analysis")]
    public DrawingAnalysisDto Analysis { get; set; } = new();

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class TechProcessDrawingTimesDto
{
    [JsonPropertyName("matches")]
    public bool? Matches { get; set; }

    [JsonPropertyName("drawing_what")]
    public string? DrawingWhat { get; set; }

    [JsonPropertyName("mismatch_reason")]
    public string? MismatchReason { get; set; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("operations")]
    public List<TechProcessOperationTimeFillDto> Operations { get; set; } = new();
}

public class TechProcessOperationTimeFillDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("op_no")]
    public int? OpNo { get; set; }

    [JsonPropertyName("setup_time_min")]
    public double? SetupTimeMin { get; set; }

    [JsonPropertyName("process_time_min")]
    public double? ProcessTimeMin { get; set; }

    [JsonPropertyName("service_time_min")]
    public double? ServiceTimeMin { get; set; }
}

public class AnalyzeTechProcessDrawingResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    [JsonPropertyName("updated_count")]
    public int UpdatedCount { get; set; }

    [JsonPropertyName("matches")]
    public bool Matches { get; set; } = true;

    [JsonPropertyName("drawing_what")]
    public string? DrawingWhat { get; set; }

    [JsonPropertyName("mismatch_reason")]
    public string? MismatchReason { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("model_name")]
    public string? ModelName { get; set; }

    [JsonPropertyName("tech_process")]
    public object? TechProcess { get; set; }
}
