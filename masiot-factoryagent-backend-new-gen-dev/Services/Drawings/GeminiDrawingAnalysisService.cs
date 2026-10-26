using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Backend.Services.Drawings;

public class GeminiDrawingAnalysisService : IDrawingAnalysisService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff", ".bmp"
    };

    private static readonly Dictionary<string, string> ExtToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".bmp"] = "image/bmp"
    };

    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IAppSettingsService _appSettings;
    private readonly ILogger<GeminiDrawingAnalysisService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GeminiDrawingAnalysisService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        IAppSettingsService appSettings,
        ILogger<GeminiDrawingAnalysisService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<DrawingAnalysisDto> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                "Поддерживаются: PDF, PNG, JPG, WEBP, TIFF, BMP. DWG/DXF сначала экспортируйте в PDF.");
        }

        var provider = await _appSettings.GetAiProviderAsync(cancellationToken);
        var apiKey = await _appSettings.GetAiApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var help = provider == "gemini"
                ? "https://aistudio.google.com/apikey"
                : "https://www.orcarouter.ai/ (ключ API в кабинете)";
            throw new InvalidOperationException(
                $"Не задан API key. Откройте Настройки → Нейросеть, вставьте ключ ({help}) и сохраните.");
        }

        var model = await _appSettings.GetAiModelAsync(cancellationToken);
        var baseUrl = await _appSettings.GetAiBaseUrlAsync(cancellationToken);
        var maxBytes = _config.GetValue("Ai:MaxFileBytes",
            _config.GetValue("Gemini:MaxFileBytes", 20_000_000));
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        if (ms.Length == 0)
            throw new InvalidOperationException("Пустой файл.");
        if (ms.Length > maxBytes)
            throw new InvalidOperationException($"Файл слишком большой (макс. {maxBytes / 1_000_000} МБ).");

        var mime = ResolveMime(contentType, ext);
        var base64 = Convert.ToBase64String(ms.ToArray());

        DrawingLaborEstimateDto? estimate = null;
        string? rawJson = null;
        string status = "completed";
        string? error = null;

        try
        {
            rawJson = provider == "gemini"
                ? await CallGeminiAsync(apiKey, model, mime, base64, cancellationToken)
                : await CallOpenAiCompatibleAsync(apiKey, baseUrl, model, mime, base64, cancellationToken);
            estimate = ParseEstimate(rawJson);
            if (estimate == null)
                throw new InvalidOperationException("Модель вернула ответ без распознаваемого JSON.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drawing analysis failed for {File}", fileName);
            status = "error";
            error = ex.Message;
        }

        var entity = new DrawingAnalysis
        {
            FileName = Path.GetFileName(fileName),
            ContentType = mime,
            Status = status,
            PartName = estimate?.PartName,
            DrawingNumber = estimate?.DrawingNumber,
            Material = estimate?.Material,
            TotalLaborHours = estimate?.TotalLaborHours,
            Confidence = estimate?.Confidence,
            ResultJson = estimate != null
                ? JsonSerializer.Serialize(estimate, JsonOpts)
                : (rawJson ?? "{}"),
            ModelName = model,
            ErrorMessage = error,
            CreatedAt = DateTime.UtcNow
        };

        _db.DrawingAnalyses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (status == "error")
            throw new InvalidOperationException(error ?? "Ошибка анализа чертежа");

        return Map(entity, estimate);
    }

    public async Task<IReadOnlyList<DrawingAnalysisDto>> GetHistoryAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.DrawingAnalyses
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);

        return rows.Select(r => Map(r, TryDeserialize(r.ResultJson))).ToList();
    }

    public async Task<DrawingAnalysisDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.DrawingAnalyses.FindAsync(new object[] { id }, cancellationToken);
        return row == null ? null : Map(row, TryDeserialize(row.ResultJson));
    }

    private static string ResolveMime(string? contentType, string ext)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return contentType;

        return ExtToMime.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }

    /// <summary>
    /// OrcaRouter / OpenAI-compatible chat.completions with vision (GLM free etc.).
    /// </summary>
    private async Task<string> CallOpenAiCompatibleAsync(
        string apiKey,
        string baseUrl,
        string model,
        string mime,
        string base64,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("OrcaRouter");
        var url = $"{baseUrl.TrimEnd('/')}/chat/completions";
        var dataUrl = $"data:{mime};base64,{base64}";

        var payload = new
        {
            model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = DrawingLaborPrompt.SystemInstruction },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = DrawingLaborPrompt.UserAsk },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = dataUrl }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("OrcaRouter HTTP {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 800));
            throw new InvalidOperationException(FormatOpenAiError(resp.StatusCode, body));
        }

        using var doc = JsonDocument.Parse(body);
        var text = ExtractOpenAiText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Пустой ответ от модели (OrcaRouter).");

        return text;
    }

    private async Task<string> CallGeminiAsync(
        string apiKey,
        string model,
        string mime,
        string base64,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = DrawingLaborPrompt.SystemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = DrawingLaborPrompt.UserAsk },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mime,
                                data = base64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini HTTP {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 800));
            throw new InvalidOperationException(FormatGeminiError(resp.StatusCode, body));
        }

        using var doc = JsonDocument.Parse(body);
        var text = ExtractGeminiText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Пустой ответ от Gemini.");

        return text;
    }

    private static string ExtractGeminiText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;

        var content = candidates[0].GetProperty("content");
        if (!content.TryGetProperty("parts", out var parts))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var t))
                sb.Append(t.GetString());
        }
        return sb.ToString();
    }

    private static string ExtractOpenAiText(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return string.Empty;

        var msg = choices[0].GetProperty("message");
        if (msg.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String)
                return content.GetString() ?? string.Empty;

            // Some models return content as array of parts
            if (content.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var t))
                        sb.Append(t.GetString());
                }
                return sb.ToString();
            }
        }
        return string.Empty;
    }

    private static DrawingLaborEstimateDto? ParseEstimate(string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json == null) return null;
        return JsonSerializer.Deserialize<DrawingLaborEstimateDto>(json, JsonOpts);
    }

    private static DrawingLaborEstimateDto? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<DrawingLaborEstimateDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            trimmed = Regex.Replace(trimmed, @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            trimmed = Regex.Replace(trimmed, @"\s*```$", "");
        }

        if (trimmed.StartsWith('{'))
            return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed.Substring(start, end - start + 1);

        return null;
    }

    private static string FormatGeminiError(System.Net.HttpStatusCode code, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                var m = msg.GetString() ?? body;
                if ((int)code == 429)
                    return $"Лимит бесплатного API Gemini исчерпан. Подождите или проверьте квоту. ({m})";
                if ((int)code == 400 && m.Contains("API key", StringComparison.OrdinalIgnoreCase))
                    return $"Неверный Gemini API key. Проверьте ключ на https://aistudio.google.com/apikey ({m})";
                return $"Gemini: {m}";
            }
        }
        catch { /* ignore */ }

        return $"Gemini HTTP {(int)code}: {Truncate(body, 300)}";
    }

    private static string FormatOpenAiError(System.Net.HttpStatusCode code, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                var m = err.TryGetProperty("message", out var msg) ? msg.GetString() : body;
                var reason = err.TryGetProperty("metadata", out var meta) &&
                             meta.TryGetProperty("reason", out var r)
                    ? r.GetString()
                    : null;

                if (reason == "err_free_rate" || (int)code == 429)
                    return $"Лимит бесплатного OrcaRouter/GLM. Подождите и повторите. ({m})";
                if (reason == "err_free_prompt_cap")
                    return $"Чертёж/промпт слишком большой для бесплатного лимита OrcaRouter. Уменьшите файл или используйте PNG. ({m})";
                if (reason == "err_free_access_denied")
                    return $"OrcaRouter: привяжите GitHub-аккаунт в профиле или пополните баланс. ({m})";
                if ((int)code == 401)
                    return $"Неверный OrcaRouter API key. Проверьте ключ в Настройки → Нейросеть. ({m})";
                return $"OrcaRouter: {m}";
            }
        }
        catch { /* ignore */ }

        return $"OrcaRouter HTTP {(int)code}: {Truncate(body, 300)}";
    }

    private static string Truncate(string s, int n) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s.Substring(0, n) + "…");

    private static DrawingAnalysisDto Map(DrawingAnalysis e, DrawingLaborEstimateDto? result) => new()
    {
        Id = e.Id,
        FileName = e.FileName,
        ContentType = e.ContentType,
        Status = e.Status,
        PartName = e.PartName ?? result?.PartName,
        DrawingNumber = e.DrawingNumber ?? result?.DrawingNumber,
        Material = e.Material ?? result?.Material,
        TotalLaborHours = e.TotalLaborHours ?? result?.TotalLaborHours,
        Confidence = e.Confidence ?? result?.Confidence,
        Result = result,
        ResultJson = e.ResultJson,
        ModelName = e.ModelName,
        ErrorMessage = e.ErrorMessage,
        CreatedAt = e.CreatedAt
    };
}
