using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/blank-okpd2")]
public class BlankOkpd2Controller : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BlankOkpd2Controller> _logger;

    public BlankOkpd2Controller(ApplicationDbContext context, ILogger<BlankOkpd2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить код ОКПД-2 по названию: сначала справочник, потом существующие агенты
    /// </summary>
    [HttpGet("find-by-name")]
    public async Task<ActionResult<BlankOkpd2Response>> FindOkpd2ByBlankName([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Название заготовки не указано");
        }

        var nameLower = name.ToLower();

        // 1. Ищем в справочнике blank_okpd2_mapping
        var mapping = await _context.BlankOkpd2Mappings
            .Where(m => m.Keywords != null && nameLower.Contains(m.Keywords.ToLower()))
            .OrderByDescending(m => m.Keywords.Length)
            .FirstOrDefaultAsync();

        if (mapping != null)
        {
            return Ok(new BlankOkpd2Response
            {
                Okpd2Code = mapping.Okpd2Code,
                Description = mapping.Description,
                MatchedKeywords = mapping.Keywords,
                Source = "dictionary"
            });
        }

        // 2. Ищем среди существующих агентов по совпадению имени
        var agent = await _context.Agents
            .Where(a => a.Name != null && a.Name.ToLower().Contains(nameLower))
            .OrderByDescending(a => a.Name!.Length)
            .FirstOrDefaultAsync();

        if (agent != null && !string.IsNullOrEmpty(agent.Okpd2Code))
        {
            return Ok(new BlankOkpd2Response
            {
                Okpd2Code = agent.Okpd2Code,
                Description = $"Найдено по агенту: {agent.Name}",
                MatchedKeywords = agent.Name,
                Source = "agent"
            });
        }

        return Ok(new BlankOkpd2Response
        {
            Okpd2Code = null,
            Description = "Код ОКПД-2 не найден. Введите код вручную.",
            MatchedKeywords = null,
            Source = "none"
        });
    }

    /// <summary>
    /// Получить все записи справочника ОКПД-2
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<List<BlankOkpd2Dto>>> GetAllMappings()
    {
        try
        {
            var mappings = await _context.BlankOkpd2Mappings
                .OrderBy(m => m.Okpd2Code)
                .Select(m => new BlankOkpd2Dto
                {
                    Id = m.Id,
                    Keywords = m.Keywords,
                    Okpd2Code = m.Okpd2Code,
                    Description = m.Description,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения всех записей ОКПД-2");
            return StatusCode(500, new { message = "Ошибка получения записей ОКПД-2" });
        }
    }

    /// <summary>
    /// Получить запись по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BlankOkpd2Dto>> GetMapping(int id)
    {
        try
        {
            var mapping = await _context.BlankOkpd2Mappings.FindAsync(id);

            if (mapping == null)
            {
                return NotFound(new { message = $"Запись с ID {id} не найдена" });
            }

            return Ok(new BlankOkpd2Dto
            {
                Id = mapping.Id,
                Keywords = mapping.Keywords,
                Okpd2Code = mapping.Okpd2Code,
                Description = mapping.Description,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения записи по ID {Id}", id);
            return StatusCode(500, new { message = "Ошибка получения записи" });
        }
    }

    /// <summary>
    /// Получить записи по коду ОКПД-2
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<List<BlankOkpd2Dto>>> GetByCode(string code)
    {
        try
        {
            var mappings = await _context.BlankOkpd2Mappings
                .Where(m => m.Okpd2Code == code)
                .Select(m => new BlankOkpd2Dto
                {
                    Id = m.Id,
                    Keywords = m.Keywords,
                    Okpd2Code = m.Okpd2Code,
                    Description = m.Description,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения записей по коду {Code}", code);
            return StatusCode(500, new { message = "Ошибка получения записей" });
        }
    }

    /// <summary>
    /// Создать новую запись
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BlankOkpd2Dto>> CreateMapping([FromBody] CreateBlankOkpd2Dto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mapping = new BlankOkpd2Mapping
            {
                Keywords = createDto.Keywords,
                Okpd2Code = createDto.Okpd2Code,
                Description = createDto.Description
            };

            _context.BlankOkpd2Mappings.Add(mapping);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMapping), new { id = mapping.Id }, new BlankOkpd2Dto
            {
                Id = mapping.Id,
                Keywords = mapping.Keywords,
                Okpd2Code = mapping.Okpd2Code,
                Description = mapping.Description,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка создания записи ОКПД-2");
            return StatusCode(500, new { message = "Ошибка создания записи ОКПД-2" });
        }
    }

    /// <summary>
    /// Массово добавить записи справочника ОКПД-2 без дублей
    /// </summary>
    [HttpPost("bulk-upsert")]
    public async Task<ActionResult<BulkBlankOkpd2ResultDto>> BulkUpsertMappings([FromBody] BulkBlankOkpd2RequestDto request)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return Ok(new BulkBlankOkpd2ResultDto());
            }

            var created = 0;
            var skipped = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in request.Items)
            {
                var keywords = (item.Keywords ?? string.Empty).Trim();
                var description = string.IsNullOrWhiteSpace(item.Description)
                    ? "Автоматически добавлено из XLSX"
                    : item.Description.Trim();

                if (keywords.Length > 255)
                {
                    keywords = keywords[..255];
                }

                var codes = (item.Okpd2Code ?? string.Empty)
                    .Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(code => !string.IsNullOrWhiteSpace(code) && code != "00.00.00.000")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (string.IsNullOrWhiteSpace(keywords) || codes.Count == 0)
                {
                    skipped++;
                    continue;
                }

                foreach (var code in codes)
                {
                    var key = $"{code.ToLowerInvariant()}|{keywords.ToLowerInvariant()}";
                    if (!seen.Add(key))
                    {
                        skipped++;
                        continue;
                    }

                    var exists = await _context.BlankOkpd2Mappings
                        .AnyAsync(m => m.Okpd2Code == code && m.Keywords.ToLower() == keywords.ToLower());

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    _context.BlankOkpd2Mappings.Add(new BlankOkpd2Mapping
                    {
                        Keywords = keywords,
                        Okpd2Code = code,
                        Description = description
                    });
                    created++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new BulkBlankOkpd2ResultDto
            {
                Created = created,
                Skipped = skipped
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка массового сохранения ОКПД-2");
            return StatusCode(500, new { message = "Ошибка массового сохранения ОКПД-2" });
        }
    }

    /// <summary>
    /// Обновить запись
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<BlankOkpd2Dto>> UpdateMapping(int id, [FromBody] UpdateBlankOkpd2Dto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mapping = await _context.BlankOkpd2Mappings.FindAsync(id);

            if (mapping == null)
            {
                return NotFound(new { message = $"Запись с ID {id} не найдена" });
            }

            if (updateDto.Keywords != null)
                mapping.Keywords = updateDto.Keywords;

            if (updateDto.Okpd2Code != null)
                mapping.Okpd2Code = updateDto.Okpd2Code;

            if (updateDto.Description != null)
                mapping.Description = updateDto.Description;

            mapping.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new BlankOkpd2Dto
            {
                Id = mapping.Id,
                Keywords = mapping.Keywords,
                Okpd2Code = mapping.Okpd2Code,
                Description = mapping.Description,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления записи {Id}", id);
            return StatusCode(500, new { message = "Ошибка обновления записи" });
        }
    }

    /// <summary>
    /// Удалить запись
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMapping(int id)
    {
        try
        {
            var mapping = await _context.BlankOkpd2Mappings.FindAsync(id);

            if (mapping == null)
            {
                return NotFound(new { message = $"Запись с ID {id} не найдена" });
            }

            _context.BlankOkpd2Mappings.Remove(mapping);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запись успешно удалена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления записи {Id}", id);
            return StatusCode(500, new { message = "Ошибка удаления записи" });
        }
    }
}

public class BlankOkpd2Response
{
    public string? Okpd2Code { get; set; }
    public string? Description { get; set; }
    public string? MatchedKeywords { get; set; }
    public string? Source { get; set; } // "dictionary", "agent", "none"
}

public class BlankOkpd2Dto
{
    public int Id { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public string Okpd2Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBlankOkpd2Dto
{
    public string Keywords { get; set; } = string.Empty;
    public string Okpd2Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateBlankOkpd2Dto
{
    public string? Keywords { get; set; }
    public string? Okpd2Code { get; set; }
    public string? Description { get; set; }
}

public class BulkBlankOkpd2RequestDto
{
    public List<CreateBlankOkpd2Dto> Items { get; set; } = new();
}

public class BulkBlankOkpd2ResultDto
{
    public int Created { get; set; }
    public int Skipped { get; set; }
}
