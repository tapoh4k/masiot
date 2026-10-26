using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/okpd2-dictionary")]
public class Okpd2DictionaryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<Okpd2DictionaryController> _logger;

    public Okpd2DictionaryController(ApplicationDbContext context, ILogger<Okpd2DictionaryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/okpd2-dictionary
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Okpd2DictionaryDto>>> GetAllOkpd2Codes()
    {
        try
        {
            var okpd2Codes = await _context.Okpd2Dictionary
                .Where(o => o.IsActive)
                .OrderBy(o => o.Code)
                .Select(o => new Okpd2DictionaryDto
                {
                    Id = o.Id,
                    Code = o.Code,
                    Name = o.Name,
                    Description = o.Description,
                    ParentCode = o.ParentCode,
                    Level = o.Level,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(okpd2Codes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all OKPD-2 codes");
            return StatusCode(500, new { message = "Error retrieving OKPD-2 codes" });
        }
    }

    // GET: api/okpd2-dictionary/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Okpd2DictionaryDto>> GetOkpd2Code(int id)
    {
        try
        {
            var okpd2Code = await _context.Okpd2Dictionary.FindAsync(id);

            if (okpd2Code == null)
            {
                return NotFound(new { message = $"OKPD-2 code with id {id} not found" });
            }

            var dto = new Okpd2DictionaryDto
            {
                Id = okpd2Code.Id,
                Code = okpd2Code.Code,
                Name = okpd2Code.Name,
                Description = okpd2Code.Description,
                ParentCode = okpd2Code.ParentCode,
                Level = okpd2Code.Level,
                IsActive = okpd2Code.IsActive,
                CreatedAt = okpd2Code.CreatedAt,
                UpdatedAt = okpd2Code.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OKPD-2 code {Id}", id);
            return StatusCode(500, new { message = "Error retrieving OKPD-2 code" });
        }
    }

    // GET: api/okpd2-dictionary/by-code/{code}
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<Okpd2DictionaryDto>> GetOkpd2CodeByCode(string code)
    {
        try
        {
            var okpd2Code = await _context.Okpd2Dictionary
                .FirstOrDefaultAsync(o => o.Code == code);

            if (okpd2Code == null)
            {
                return NotFound(new { message = $"OKPD-2 code {code} not found" });
            }

            var dto = new Okpd2DictionaryDto
            {
                Id = okpd2Code.Id,
                Code = okpd2Code.Code,
                Name = okpd2Code.Name,
                Description = okpd2Code.Description,
                ParentCode = okpd2Code.ParentCode,
                Level = okpd2Code.Level,
                IsActive = okpd2Code.IsActive,
                CreatedAt = okpd2Code.CreatedAt,
                UpdatedAt = okpd2Code.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OKPD-2 code by code {Code}", code);
            return StatusCode(500, new { message = "Error retrieving OKPD-2 code" });
        }
    }

    // GET: api/okpd2-dictionary/children/{parentCode}
    [HttpGet("children/{parentCode}")]
    public async Task<ActionResult<IEnumerable<Okpd2DictionaryDto>>> GetChildrenByParentCode(string parentCode)
    {
        try
        {
            var children = await _context.Okpd2Dictionary
                .Where(o => o.ParentCode == parentCode && o.IsActive)
                .OrderBy(o => o.Code)
                .Select(o => new Okpd2DictionaryDto
                {
                    Id = o.Id,
                    Code = o.Code,
                    Name = o.Name,
                    Description = o.Description,
                    ParentCode = o.ParentCode,
                    Level = o.Level,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children for parent code {ParentCode}", parentCode);
            return StatusCode(500, new { message = "Error retrieving children codes" });
        }
    }

    // POST: api/okpd2-dictionary
    [HttpPost]
    public async Task<ActionResult<Okpd2DictionaryDto>> CreateOkpd2Code([FromBody] CreateOkpd2DictionaryDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var okpd2Code = new Okpd2Dictionary
            {
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                ParentCode = createDto.ParentCode,
                Level = createDto.Level,
                IsActive = createDto.IsActive
            };

            _context.Okpd2Dictionary.Add(okpd2Code);
            await _context.SaveChangesAsync();

            var dto = new Okpd2DictionaryDto
            {
                Id = okpd2Code.Id,
                Code = okpd2Code.Code,
                Name = okpd2Code.Name,
                Description = okpd2Code.Description,
                ParentCode = okpd2Code.ParentCode,
                Level = okpd2Code.Level,
                IsActive = okpd2Code.IsActive,
                CreatedAt = okpd2Code.CreatedAt,
                UpdatedAt = okpd2Code.UpdatedAt
            };

            return CreatedAtAction(nameof(GetOkpd2Code), new { id = okpd2Code.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OKPD-2 code");
            return StatusCode(500, new { message = "Error creating OKPD-2 code" });
        }
    }

    // PUT: api/okpd2-dictionary/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Okpd2DictionaryDto>> UpdateOkpd2Code(int id, [FromBody] UpdateOkpd2DictionaryDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var okpd2Code = await _context.Okpd2Dictionary.FindAsync(id);

            if (okpd2Code == null)
            {
                return NotFound(new { message = $"OKPD-2 code with id {id} not found" });
            }

            if (updateDto.Name != null)
                okpd2Code.Name = updateDto.Name;

            if (updateDto.Description != null)
                okpd2Code.Description = updateDto.Description;

            if (updateDto.ParentCode != null)
                okpd2Code.ParentCode = updateDto.ParentCode;

            if (updateDto.Level.HasValue)
                okpd2Code.Level = updateDto.Level.Value;

            if (updateDto.IsActive.HasValue)
                okpd2Code.IsActive = updateDto.IsActive.Value;

            await _context.SaveChangesAsync();

            var dto = new Okpd2DictionaryDto
            {
                Id = okpd2Code.Id,
                Code = okpd2Code.Code,
                Name = okpd2Code.Name,
                Description = okpd2Code.Description,
                ParentCode = okpd2Code.ParentCode,
                Level = okpd2Code.Level,
                IsActive = okpd2Code.IsActive,
                CreatedAt = okpd2Code.CreatedAt,
                UpdatedAt = okpd2Code.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating OKPD-2 code {Id}", id);
            return StatusCode(500, new { message = "Error updating OKPD-2 code" });
        }
    }

    // DELETE: api/okpd2-dictionary/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOkpd2Code(int id)
    {
        try
        {
            var okpd2Code = await _context.Okpd2Dictionary.FindAsync(id);

            if (okpd2Code == null)
            {
                return NotFound(new { message = $"OKPD-2 code with id {id} not found" });
            }

            _context.Okpd2Dictionary.Remove(okpd2Code);
            await _context.SaveChangesAsync();

            return Ok(new { message = "OKPD-2 code deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting OKPD-2 code {Id}", id);
            return StatusCode(500, new { message = "Error deleting OKPD-2 code" });
        }
    }
}
