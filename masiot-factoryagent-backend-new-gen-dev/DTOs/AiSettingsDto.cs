using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class AiSettingsDto
{
    /// <summary>orcarouter | gemini</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "orcarouter";

    [JsonPropertyName("has_api_key")]
    public bool HasApiKey { get; set; }

    /// <summary>Masked hint like ••••abcd — never the full key.</summary>
    [JsonPropertyName("api_key_hint")]
    public string? ApiKeyHint { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "z-ai/glm-5.3-flash-free";

    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("help_url")]
    public string? HelpUrl { get; set; }
}

public class UpdateAiSettingsDto
{
    /// <summary>orcarouter | gemini</summary>
    [JsonPropertyName("provider")]
    [MaxLength(32)]
    public string? Provider { get; set; }

    /// <summary>New API key. Omit or empty to leave existing key unchanged.</summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("model")]
    [MaxLength(128)]
    public string? Model { get; set; }

    [JsonPropertyName("base_url")]
    [MaxLength(256)]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("clear_api_key")]
    public bool ClearApiKey { get; set; }
}
