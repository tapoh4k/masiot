using Backend.DTOs;

namespace Backend.Services.Settings;

public interface IAppSettingsService
{
    Task<AiSettingsDto> GetAiSettingsAsync(CancellationToken cancellationToken = default);
    Task<AiSettingsDto> UpdateAiSettingsAsync(UpdateAiSettingsDto dto, CancellationToken cancellationToken = default);

    Task<string> GetAiProviderAsync(CancellationToken cancellationToken = default);
    Task<string?> GetAiApiKeyAsync(CancellationToken cancellationToken = default);
    Task<string> GetAiModelAsync(CancellationToken cancellationToken = default);
    Task<string> GetAiBaseUrlAsync(CancellationToken cancellationToken = default);

    // Back-compat aliases used by drawing service
    Task<string?> GetGeminiApiKeyAsync(CancellationToken cancellationToken = default);
    Task<string> GetGeminiModelAsync(CancellationToken cancellationToken = default);
}
