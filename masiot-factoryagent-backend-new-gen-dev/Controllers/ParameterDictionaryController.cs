using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/parameter-dictionary")]
public class ParameterDictionaryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParameterDictionaryController> _logger;

    public ParameterDictionaryController(ApplicationDbContext context, ILogger<ParameterDictionaryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/parameter-dictionary
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParameterDictionaryDto>>> GetAllParameters()
    {
        try
        {
            var parameters = await _context.ParameterDictionary
                .Select(p => new ParameterDictionaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Unit = p.Unit,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parameters");
            return StatusCode(500, new { message = "Error retrieving parameters" });
        }
    }

    // GET: api/parameter-dictionary/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ParameterDictionaryDto>> GetParameter(int id)
    {
        try
        {
            var parameter = await _context.ParameterDictionary.FindAsync(id);

            if (parameter == null)
            {
                return NotFound(new { message = $"Parameter with id {id} not found" });
            }

            var dto = new ParameterDictionaryDto
            {
                Id = parameter.Id,
                Name = parameter.Name,
                Description = parameter.Description,
                Type = parameter.Type,
                Unit = parameter.Unit,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parameter {Id}", id);
            return StatusCode(500, new { message = "Error retrieving parameter" });
        }
    }

    // POST: api/parameter-dictionary
    [HttpPost]
    public async Task<ActionResult<ParameterDictionaryDto>> CreateParameter([FromBody] CreateParameterDictionaryDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var parameter = new ParameterDictionary
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Type = createDto.Type,
                Unit = createDto.Unit
            };

            _context.ParameterDictionary.Add(parameter);
            await _context.SaveChangesAsync();

            var dto = new ParameterDictionaryDto
            {
                Id = parameter.Id,
                Name = parameter.Name,
                Description = parameter.Description,
                Type = parameter.Type,
                Unit = parameter.Unit,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };

            return CreatedAtAction(nameof(GetParameter), new { id = parameter.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parameter");
            return StatusCode(500, new { message = "Error creating parameter" });
        }
    }

    // PUT: api/parameter-dictionary/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ParameterDictionaryDto>> UpdateParameter(int id, [FromBody] UpdateParameterDictionaryDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var parameter = await _context.ParameterDictionary.FindAsync(id);

            if (parameter == null)
            {
                return NotFound(new { message = $"Parameter with id {id} not found" });
            }

            if (updateDto.Name != null)
                parameter.Name = updateDto.Name;

            if (updateDto.Description != null)
                parameter.Description = updateDto.Description;

            if (updateDto.Type != null)
                parameter.Type = updateDto.Type;

            if (updateDto.Unit != null)
                parameter.Unit = updateDto.Unit;

            await _context.SaveChangesAsync();

            var dto = new ParameterDictionaryDto
            {
                Id = parameter.Id,
                Name = parameter.Name,
                Description = parameter.Description,
                Type = parameter.Type,
                Unit = parameter.Unit,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parameter {Id}", id);
            return StatusCode(500, new { message = "Error updating parameter" });
        }
    }

    // DELETE: api/parameter-dictionary/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteParameter(int id)
    {
        try
        {
            var parameter = await _context.ParameterDictionary.FindAsync(id);

            if (parameter == null)
            {
                return NotFound(new { message = $"Parameter with id {id} not found" });
            }

            _context.ParameterDictionary.Remove(parameter);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Parameter deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parameter {Id}", id);
            return StatusCode(500, new { message = "Error deleting parameter" });
        }
    }
}
