using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Settings;

public class AppSettingsService : IAppSettingsService
{
    public const string ProviderKey = "ai_provider";
    public const string ApiKeyKey = "ai_api_key";
    public const string ApiKeyHintKey = "ai_api_key_hint";
    public const string ModelKey = "ai_model";
    public const string BaseUrlKey = "ai_base_url";

    // Legacy keys (Gemini-only era)
    public const string LegacyGeminiApiKeyKey = "gemini_api_key";
    public const string LegacyGeminiModelKey = "gemini_model";
    public const string LegacyGeminiApiKeyHintKey = "gemini_api_key_hint";

    public const string DefaultProvider = "orcarouter";
    public const string DefaultOrcaModel = "z-ai/glm-5.3-flash-free";
    public const string DefaultOrcaBaseUrl = "https://api.orcarouter.ai/v1";
    public const string DefaultGeminiModel = "gemini-2.5-flash";

    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _config;

    public AppSettingsService(
        ApplicationDbContext db,
        IEncryptionService encryption,
        IConfiguration config)
    {
        _db = db;
        _encryption = encryption;
        _config = config;
    }

    public async Task<AiSettingsDto> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        var provider = await GetAiProviderAsync(cancellationToken);
        var model = await GetAiModelAsync(cancellationToken);
        var baseUrl = await GetAiBaseUrlAsync(cancellationToken);

        var encKey = await GetEncryptedKeyRowAsync(cancellationToken);
        var hintRow = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ApiKeyHintKey || s.Key == LegacyGeminiApiKeyHintKey, cancellationToken);

        var hasDbKey = !string.IsNullOrWhiteSpace(encKey?.Value);
        var hasEnvKey = !string.IsNullOrWhiteSpace(ResolveConfigApiKey(provider));

        return new AiSettingsDto
        {
            Provider = provider,
            HasApiKey = hasDbKey || hasEnvKey,
            ApiKeyHint = hasDbKey
                ? (hintRow?.Value ?? "ключ сохранён")
                : (hasEnvKey ? "из конфигурации сервера" : null),
            Model = model,
            BaseUrl = baseUrl,
            UpdatedAt = encKey?.UpdatedAt,
            HelpUrl = provider == "gemini"
                ? "https://aistudio.google.com/apikey"
                : "https://www.orcarouter.ai/"
        };
    }

    public async Task<AiSettingsDto> UpdateAiSettingsAsync(
        UpdateAiSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var providerChanged = false;
        if (!string.IsNullOrWhiteSpace(dto.Provider))
        {
            var p = NormalizeProvider(dto.Provider);
            await UpsertAsync(ProviderKey, p, isSecret: false, cancellationToken);
            providerChanged = true;
        }

        if (dto.ClearApiKey)
        {
            await UpsertAsync(ApiKeyKey, null, isSecret: true, cancellationToken);
            await UpsertAsync(ApiKeyHintKey, null, isSecret: false, cancellationToken);
            await UpsertAsync(LegacyGeminiApiKeyKey, null, isSecret: true, cancellationToken);
            await UpsertAsync(LegacyGeminiApiKeyHintKey, null, isSecret: false, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            var plain = dto.ApiKey.Trim();
            var encrypted = _encryption.Encrypt(plain);
            var hint = MaskKey(plain);
            await UpsertAsync(ApiKeyKey, encrypted, isSecret: true, cancellationToken);
            await UpsertAsync(ApiKeyHintKey, hint, isSecret: false, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(dto.Model))
        {
            await UpsertAsync(ModelKey, dto.Model.Trim(), isSecret: false, cancellationToken);
        }
        else if (providerChanged && string.IsNullOrWhiteSpace(dto.Model))
        {
            // Switch default model when provider changes and model not explicitly set
            var p = NormalizeProvider(dto.Provider!);
            await UpsertAsync(ModelKey,
                p == "gemini" ? DefaultGeminiModel : DefaultOrcaModel,
                isSecret: false, cancellationToken);
        }

        if (dto.BaseUrl != null)
        {
            var url = dto.BaseUrl.Trim().TrimEnd('/');
            await UpsertAsync(BaseUrlKey, string.IsNullOrWhiteSpace(url) ? null : url, isSecret: false, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetAiSettingsAsync(cancellationToken);
    }

    public async Task<string> GetAiProviderAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ProviderKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(row?.Value))
            return NormalizeProvider(row.Value);

        var cfg = _config["Ai:Provider"] ?? _config["Gemini:Provider"];
        return NormalizeProvider(cfg ?? DefaultProvider);
    }

    public async Task<string?> GetAiApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ApiKeyKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(row?.Value))
        {
            row = await _db.AppSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == LegacyGeminiApiKeyKey, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(row?.Value))
        {
            try { return _encryption.Decrypt(row.Value); }
            catch { return row.Value; }
        }

        var provider = await GetAiProviderAsync(cancellationToken);
        return ResolveConfigApiKey(provider);
    }

    public async Task<string> GetAiModelAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ModelKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(row?.Value))
            return row.Value;

        row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == LegacyGeminiModelKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(row?.Value))
            return row.Value;

        var provider = await GetAiProviderAsync(cancellationToken);
        if (provider == "gemini")
            return _config["Gemini:Model"] ?? DefaultGeminiModel;

        return _config["Ai:Model"] ?? _config["OrcaRouter:Model"] ?? DefaultOrcaModel;
    }

    public async Task<string> GetAiBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == BaseUrlKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(row?.Value))
            return row.Value.TrimEnd('/');

        var provider = await GetAiProviderAsync(cancellationToken);
        if (provider == "gemini")
            return "https://generativelanguage.googleapis.com/v1beta";

        return _config["Ai:BaseUrl"]
            ?? _config["OrcaRouter:BaseUrl"]
            ?? DefaultOrcaBaseUrl;
    }

    public Task<string?> GetGeminiApiKeyAsync(CancellationToken cancellationToken = default) =>
        GetAiApiKeyAsync(cancellationToken);

    public Task<string> GetGeminiModelAsync(CancellationToken cancellationToken = default) =>
        GetAiModelAsync(cancellationToken);

    private async Task<AppSetting?> GetEncryptedKeyRowAsync(CancellationToken ct)
    {
        var row = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ApiKeyKey, ct);
        if (!string.IsNullOrWhiteSpace(row?.Value)) return row;

        return await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == LegacyGeminiApiKeyKey, ct);
    }

    private string? ResolveConfigApiKey(string provider)
    {
        if (provider == "orcarouter")
        {
            return _config["OrcaRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("ORCAROUTER_API_KEY")
                ?? _config["Ai:ApiKey"]
                ?? Environment.GetEnvironmentVariable("AI_API_KEY");
        }

        return _config["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GoogleAI__ApiKey")
            ?? _config["Ai:ApiKey"];
    }

    private static string NormalizeProvider(string provider)
    {
        var p = provider.Trim().ToLowerInvariant();
        return p is "gemini" or "google" ? "gemini" : "orcarouter";
    }

    private async Task UpsertAsync(string key, string? value, bool isSecret, CancellationToken ct)
    {
        var row = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row == null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                IsSecret = isSecret,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = value;
            row.IsSecret = isSecret;
            row.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 4) return "••••";
        return "••••" + key[^4..];
    }
}
