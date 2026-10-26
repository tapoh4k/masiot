using Backend.DTOs;
using Backend.Services.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IAppSettingsService _settings;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IAppSettingsService settings, ILogger<SettingsController> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("ai")]
    public async Task<ActionResult<AiSettingsDto>> GetAi(CancellationToken cancellationToken)
    {
        var dto = await _settings.GetAiSettingsAsync(cancellationToken);
        return Ok(dto);
    }

    [HttpPut("ai")]
    public async Task<ActionResult<AiSettingsDto>> UpdateAi(
        [FromBody] UpdateAiSettingsDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest(new { message = "Тело запроса пустое" });

        try
        {
            var result = await _settings.UpdateAiSettingsAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update AI settings");
            return StatusCode(500, new { message = "Не удалось сохранить настройки", detail = ex.Message });
        }
    }
}
