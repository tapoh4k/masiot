using Backend.DTOs;
using Backend.Services.Drawings;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/drawings")]
public class DrawingsController : ControllerBase
{
    private readonly IDrawingAnalysisService _service;
    private readonly ILogger<DrawingsController> _logger;

    public DrawingsController(IDrawingAnalysisService service, ILogger<DrawingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Upload engineering drawing (PDF/image) and estimate labor intensity via Gemini vision.
    /// </summary>
    [HttpPost("analyze")]
    [RequestSizeLimit(25_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25_000_000)]
    public async Task<ActionResult<AnalyzeDrawingResponseDto>> Analyze(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не передан" });

        try
        {
            await using var stream = file.OpenReadStream();
            var analysis = await _service.AnalyzeAsync(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);

            return Ok(new AnalyzeDrawingResponseDto
            {
                Analysis = analysis,
                Message = "Анализ завершён"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Drawing analyze rejected");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drawing analyze failed");
            return StatusCode(500, new { message = "Ошибка анализа чертежа", detail = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DrawingAnalysisDto>>> GetHistory(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await _service.GetHistoryAsync(limit, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DrawingAnalysisDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound(new { message = "Не найдено" });
        return Ok(item);
    }

    /// <summary>
    /// Upload a drawing for a tech-process agent and fill operation times in the TP table.
    /// </summary>
    [HttpPost("analyze-tech-process/{agentId:int}")]
    [RequestSizeLimit(25_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25_000_000)]
    public async Task<ActionResult<AnalyzeTechProcessDrawingResponseDto>> AnalyzeTechProcess(
        int agentId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не передан" });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _service.AnalyzeForTechProcessAsync(
                agentId,
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "TP drawing analyze rejected for agent {AgentId}", agentId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TP drawing analyze failed for agent {AgentId}", agentId);
            return StatusCode(500, new { message = "Ошибка анализа чертежа", detail = ex.Message });
        }
    }
}
