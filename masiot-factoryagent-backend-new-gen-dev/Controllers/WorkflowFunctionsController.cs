using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using System.Text.Json;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowFunctionsController : ControllerBase
{
    // В БД params хранятся в camelCase ({"name":..,"type":..}), как в schema.sql
    private static readonly JsonSerializerOptions ParamsJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowFunctionsController> _logger;

    public WorkflowFunctionsController(ApplicationDbContext context, ILogger<WorkflowFunctionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowFunctionDto>>> GetAllWorkflowFunctions()
    {
        try
        {
            var functions = await _context.WorkflowFunctions
                .OrderBy(f => f.Name)
                .ToListAsync();

            var dtos = functions.Select(f => new WorkflowFunctionDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FunctionClass = f.FunctionClass,
                FunctionName = f.FunctionName,
                Params = ParseParams(f.Params),
                Returns = f.Returns,
                Icon = f.Icon,
                Type = f.Type,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow functions");
            return StatusCode(500, new { message = "Error retrieving workflow functions" });
        }
    }

    private List<WorkflowParameterDto> ParseParams(string paramsJson)
    {
        try
        {
            // Try to parse as new format (List<WorkflowParameterDto>)
            var paramObjects = JsonSerializer.Deserialize<List<WorkflowParameterDto>>(paramsJson, ParamsJsonOptions);
            if (paramObjects != null)
                return paramObjects;
        }
        catch
        {
            // If that fails, try old format (List<string>)
            try
            {
                var paramStrings = JsonSerializer.Deserialize<List<string>>(paramsJson);
                if (paramStrings != null)
                {
                    return paramStrings.Select(p => new WorkflowParameterDto
                    {
                        Name = p,
                        Type = "string",
                        Description = ""
                    }).ToList();
                }
            }
            catch
            {
                // If both fail, return empty list
            }
        }
        return new List<WorkflowParameterDto>();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowFunctionDto>> GetWorkflowFunction(int id)
    {
        try
        {
            var function = await _context.WorkflowFunctions.FindAsync(id);

            if (function == null)
            {
                return NotFound(new { message = "Function not found" });
            }

            var dto = new WorkflowFunctionDto
            {
                Id = function.Id,
                Name = function.Name,
                Description = function.Description,
                FunctionClass = function.FunctionClass,
                FunctionName = function.FunctionName,
                Params = ParseParams(function.Params),
                Returns = function.Returns,
                Icon = function.Icon,
                Type = function.Type,
                CreatedAt = function.CreatedAt,
                UpdatedAt = function.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow function {Id}", id);
            return StatusCode(500, new { message = "Error retrieving workflow function" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowFunctionDto>> CreateWorkflowFunction(CreateWorkflowFunctionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var function = new WorkflowFunction
            {
                Name = dto.Name,
                Description = dto.Description,
                FunctionClass = dto.FunctionClass,
                FunctionName = dto.FunctionName,
                Params = JsonSerializer.Serialize(dto.Params, ParamsJsonOptions),
                Returns = dto.Returns,
                Icon = dto.Icon,
                Type = dto.Type
            };

            _context.WorkflowFunctions.Add(function);
            await _context.SaveChangesAsync();

            var resultDto = new WorkflowFunctionDto
            {
                Id = function.Id,
                Name = function.Name,
                Description = function.Description,
                FunctionClass = function.FunctionClass,
                FunctionName = function.FunctionName,
                Params = dto.Params,
                Returns = function.Returns,
                Icon = function.Icon,
                Type = function.Type,
                CreatedAt = function.CreatedAt,
                UpdatedAt = function.UpdatedAt
            };

            return CreatedAtAction(nameof(GetWorkflowFunction), new { id = function.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow function");
            return StatusCode(500, new { message = "Error creating workflow function" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowFunctionDto>> UpdateWorkflowFunction(int id, UpdateWorkflowFunctionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var function = await _context.WorkflowFunctions.FindAsync(id);

            if (function == null)
            {
                return NotFound(new { message = "Function not found" });
            }

            function.Name = dto.Name;
            function.Description = dto.Description;
            function.FunctionClass = dto.FunctionClass;
            function.FunctionName = dto.FunctionName;
            function.Params = JsonSerializer.Serialize(dto.Params, ParamsJsonOptions);
            function.Returns = dto.Returns;
            function.Icon = dto.Icon;
            function.Type = dto.Type;

            await _context.SaveChangesAsync();

            var resultDto = new WorkflowFunctionDto
            {
                Id = function.Id,
                Name = function.Name,
                Description = function.Description,
                FunctionClass = function.FunctionClass,
                FunctionName = function.FunctionName,
                Params = dto.Params,
                Returns = function.Returns,
                Icon = function.Icon,
                Type = function.Type,
                CreatedAt = function.CreatedAt,
                UpdatedAt = function.UpdatedAt
            };

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow function {Id}", id);
            return StatusCode(500, new { message = "Error updating workflow function" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWorkflowFunction(int id)
    {
        try
        {
            var function = await _context.WorkflowFunctions.FindAsync(id);

            if (function == null)
            {
                return NotFound(new { message = "Function not found" });
            }

            _context.WorkflowFunctions.Remove(function);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Function deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow function {Id}", id);
            return StatusCode(500, new { message = "Error deleting workflow function" });
        }
    }
}
