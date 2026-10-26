using Backend.DTOs;

namespace Backend.Services.Drawings;

public interface IDrawingAnalysisService
{
    Task<DrawingAnalysisDto> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DrawingAnalysisDto>> GetHistoryAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<DrawingAnalysisDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
