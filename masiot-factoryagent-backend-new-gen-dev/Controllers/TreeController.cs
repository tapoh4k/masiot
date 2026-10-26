using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/tree")]
public class TreeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TreeController> _logger;

    public TreeController(ApplicationDbContext context, ILogger<TreeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/tree
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TreeNodeDto>>> GetAllTreeNodes()
    {
        try
        {
            var treeNodes = await _context.TreeNodes
                .Select(t => new TreeNodeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Icon = t.Icon,
                    Type = t.Type,
                    ParentId = t.ParentId,
                    Position = t.Position,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(treeNodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tree nodes");
            return StatusCode(500, new { message = "Error retrieving tree nodes" });
        }
    }

    // GET: api/tree/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TreeNodeDto>> GetTreeNode(string id)
    {
        try
        {
            var treeNode = await _context.TreeNodes.FindAsync(id);

            if (treeNode == null)
            {
                return NotFound(new { message = $"Tree node with id {id} not found" });
            }

            var dto = new TreeNodeDto
            {
                Id = treeNode.Id,
                Name = treeNode.Name,
                Icon = treeNode.Icon,
                Type = treeNode.Type,
                ParentId = treeNode.ParentId,
                Position = treeNode.Position,
                CreatedAt = treeNode.CreatedAt,
                UpdatedAt = treeNode.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tree node {Id}", id);
            return StatusCode(500, new { message = "Error retrieving tree node" });
        }
    }

    // POST: api/tree
    [HttpPost]
    public async Task<ActionResult<TreeNodeDto>> CreateTreeNode([FromBody] CreateTreeNodeDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if node with this ID already exists
            var existingNode = await _context.TreeNodes.FindAsync(createDto.Id);
            if (existingNode != null)
            {
                return BadRequest(new { message = $"Tree node with id '{createDto.Id}' already exists" });
            }

            // Validate parent exists if ParentId is provided
            if (!string.IsNullOrEmpty(createDto.ParentId))
            {
                var parentExists = await _context.TreeNodes.AnyAsync(t => t.Id == createDto.ParentId);
                if (!parentExists)
                {
                    return BadRequest(new { message = $"Parent node with id '{createDto.ParentId}' not found" });
                }
            }

            var treeNode = new TreeNode
            {
                Id = createDto.Id,
                Name = createDto.Name,
                Icon = createDto.Icon,
                Type = createDto.Type,
                ParentId = createDto.ParentId,
                Position = createDto.Position
            };

            _context.TreeNodes.Add(treeNode);
            await _context.SaveChangesAsync();

            var dto = new TreeNodeDto
            {
                Id = treeNode.Id,
                Name = treeNode.Name,
                Icon = treeNode.Icon,
                Type = treeNode.Type,
                ParentId = treeNode.ParentId,
                Position = treeNode.Position,
                CreatedAt = treeNode.CreatedAt,
                UpdatedAt = treeNode.UpdatedAt
            };

            return CreatedAtAction(nameof(GetTreeNode), new { id = treeNode.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tree node");
            return StatusCode(500, new { message = "Error creating tree node" });
        }
    }

    // PUT: api/tree/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TreeNodeDto>> UpdateTreeNode(string id, [FromBody] UpdateTreeNodeDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var treeNode = await _context.TreeNodes.FindAsync(id);

            if (treeNode == null)
            {
                return NotFound(new { message = $"Tree node with id {id} not found" });
            }

            if (updateDto.Name != null)
                treeNode.Name = updateDto.Name;

            if (updateDto.Icon != null)
                treeNode.Icon = updateDto.Icon;

            if (updateDto.Type != null)
                treeNode.Type = updateDto.Type;

            if (updateDto.ParentId != null)
                treeNode.ParentId = updateDto.ParentId;

            if (updateDto.Position.HasValue)
                treeNode.Position = updateDto.Position;

            await _context.SaveChangesAsync();

            var dto = new TreeNodeDto
            {
                Id = treeNode.Id,
                Name = treeNode.Name,
                Icon = treeNode.Icon,
                Type = treeNode.Type,
                ParentId = treeNode.ParentId,
                Position = treeNode.Position,
                CreatedAt = treeNode.CreatedAt,
                UpdatedAt = treeNode.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tree node {Id}", id);
            return StatusCode(500, new { message = "Error updating tree node" });
        }
    }

    // PUT: api/tree/{id}/parent
    // Специальный endpoint, чтобы можно было ПРЯМО поставить parent_id = null (поднять папку на верхний уровень)
    [HttpPut("{id}/parent")]
    public async Task<ActionResult<TreeNodeDto>> UpdateTreeNodeParent(string id, [FromBody] System.Text.Json.JsonElement body)
    {
        try
        {
            var treeNode = await _context.TreeNodes.FindAsync(id);
            if (treeNode == null)
            {
                return NotFound(new { message = $"Tree node with id {id} not found" });
            }

            if (!body.TryGetProperty("parent_id", out var parentProp))
            {
                return BadRequest(new { message = "parent_id is required" });
            }

            treeNode.ParentId = parentProp.ValueKind == System.Text.Json.JsonValueKind.Null
                ? null
                : parentProp.GetString();

            await _context.SaveChangesAsync();

            var dto = new TreeNodeDto
            {
                Id = treeNode.Id,
                Name = treeNode.Name,
                Icon = treeNode.Icon,
                Type = treeNode.Type,
                ParentId = treeNode.ParentId,
                Position = treeNode.Position,
                CreatedAt = treeNode.CreatedAt,
                UpdatedAt = treeNode.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tree node parent {Id}", id);
            return StatusCode(500, new { message = "Error updating tree node parent" });
        }
    }

    // DELETE: api/tree/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTreeNode(string id)
    {
        try
        {
            var treeNode = await _context.TreeNodes.FindAsync(id);

            if (treeNode == null)
            {
                return NotFound(new { message = $"Tree node with id {id} not found" });
            }

            _context.TreeNodes.Remove(treeNode);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tree node deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tree node {Id}", id);
            return StatusCode(500, new { message = "Error deleting tree node" });
        }
    }
}
