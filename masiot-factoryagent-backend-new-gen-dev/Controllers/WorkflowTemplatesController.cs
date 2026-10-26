using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using System.Text.Json;

namespace Backend.Controllers;

[ApiController]
[Route("api/workflow-templates")]
public class WorkflowTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowTemplatesController> _logger;

    public WorkflowTemplatesController(ApplicationDbContext context, ILogger<WorkflowTemplatesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/workflow-templates
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowTemplateDto>>> GetAllWorkflowTemplates()
    {
        try
        {
            var templates = await _context.WorkflowTemplates
                .OrderBy(wt => wt.Name)
                .ToListAsync();

            var dtos = templates.Select(ToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all workflow templates");
            return StatusCode(500, new { message = "Error retrieving workflow templates" });
        }
    }

    // GET: api/workflow-templates/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowTemplateDto>> GetWorkflowTemplate(int id)
    {
        try
        {
            var template = await _context.WorkflowTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Workflow template with id {id} not found" });
            }

            return Ok(ToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow template {Id}", id);
            return StatusCode(500, new { message = "Error retrieving workflow template" });
        }
    }

    // POST: api/workflow-templates
    [HttpPost]
    public async Task<ActionResult<WorkflowTemplateDto>> CreateWorkflowTemplate([FromBody] CreateWorkflowTemplateDto createDto)
    {
        try
        {
            var name = createDto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Название шаблона обязательно" });
            }

            var exists = await _context.WorkflowTemplates
                .AnyAsync(t => t.Name.ToLower() == name.ToLower());
            if (exists)
            {
                return Conflict(new { message = $"Шаблон с именем «{name}» уже существует" });
            }

            var template = new WorkflowTemplate
            {
                Name = name,
                Description = createDto.Description,
                Nodes = createDto.Nodes.HasValue ? createDto.Nodes.Value.GetRawText() : "[]",
                Edges = createDto.Edges.HasValue ? createDto.Edges.Value.GetRawText() : "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.WorkflowTemplates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkflowTemplate), new { id = template.Id }, ToDto(template));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Шаблон с именем «{createDto.Name}» уже существует" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow template");
            return StatusCode(500, new { message = "Error creating workflow template" });
        }
    }

    // PUT: api/workflow-templates/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowTemplateDto>> UpdateWorkflowTemplate(int id, [FromBody] UpdateWorkflowTemplateDto updateDto)
    {
        try
        {
            var template = await _context.WorkflowTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Workflow template with id {id} not found" });
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
            {
                var name = updateDto.Name.Trim();
                var nameTaken = await _context.WorkflowTemplates
                    .AnyAsync(t => t.Id != id && t.Name.ToLower() == name.ToLower());
                if (nameTaken)
                {
                    return Conflict(new { message = $"Шаблон с именем «{name}» уже существует" });
                }

                template.Name = name;
            }

            if (updateDto.Description != null)
            {
                template.Description = updateDto.Description;
            }

            if (updateDto.Nodes.HasValue)
            {
                template.Nodes = updateDto.Nodes.Value.GetRawText();
            }

            if (updateDto.Edges.HasValue)
            {
                template.Edges = updateDto.Edges.Value.GetRawText();
            }

            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToDto(template));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Шаблон с именем «{updateDto.Name}» уже существует" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow template {Id}", id);
            return StatusCode(500, new { message = "Error updating workflow template" });
        }
    }

    // DELETE: api/workflow-templates/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWorkflowTemplate(int id)
    {
        try
        {
            var template = await _context.WorkflowTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Workflow template with id {id} not found" });
            }

            _context.WorkflowTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Workflow template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow template {Id}", id);
            return StatusCode(500, new { message = "Error deleting workflow template" });
        }
    }

    private static WorkflowTemplateDto ToDto(WorkflowTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        Nodes = JsonDocument.Parse(template.Nodes).RootElement,
        Edges = JsonDocument.Parse(template.Edges).RootElement,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt
    };

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("uq_workflow_templates_name", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
    }
}
