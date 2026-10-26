using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class FileUploadDto
{
    public int Id { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class CreateFileUploadDto
{
    [Required(ErrorMessage = "Имя файла обязательно")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Имя файла должно быть от 1 до 500 символов")]
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("uploaded_at")]
    public DateTime? UploadedAt { get; set; }

    [StringLength(100, ErrorMessage = "Статус не может превышать 100 символов")]
    public string? Status { get; set; }
}

public class StatisticsDto
{
    public int Id { get; set; }

    [JsonPropertyName("agents_count")]
    public int TotalAgents { get; set; }

    [JsonPropertyName("tech_process_count")]
    public int TechProcessCount { get; set; }

    [JsonPropertyName("equipment_count")]
    public int EquipmentCount { get; set; }

    [JsonPropertyName("instrument_count")]
    public int InstrumentCount { get; set; }

    [JsonPropertyName("personnel_count")]
    public int PersonnelCount { get; set; }

    [JsonPropertyName("blank_count")]
    public int BlankCount { get; set; }

    [JsonPropertyName("other_count")]
    public int OtherCount { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
