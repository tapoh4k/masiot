using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Services.Agents;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAgentLifecycleService _lifecycle;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        ApplicationDbContext context,
        IAgentLifecycleService lifecycle,
        ILogger<AgentsController> logger)
    {
        _context = context;
        _lifecycle = lifecycle;
        _logger = logger;
    }

    // GET: api/agents
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAllAgents()
    {
        try
        {
            var agents = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .Include(a => a.PendingMessages)
                .ToListAsync();

            var agentDtos = agents.Select(MapToDto).ToList();

            return Ok(agentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all agents");
            return StatusCode(500, new { message = "Error retrieving agents" });
        }
    }

    // GET: api/agents/{nodeId} (string nodeId for tree nodes)
    [HttpGet("{nodeId}")]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgentsByNodeId(string nodeId)
    {
        try
        {
            var agents = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .Where(a => a.NodeId == nodeId)
                .ToListAsync();

            return Ok(agents.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents for node {NodeId}", nodeId);
            return StatusCode(500, new { message = "Error retrieving agents" });
        }
    }

    // GET: api/agents/okpd2/{code}
    [HttpGet("okpd2/{code}")]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgentsByOkpd2Code(string code)
    {
        try
        {
            var agents = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .Where(a => a.Okpd2Code == code)
                .ToListAsync();

            return Ok(agents.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents by OKPD-2 code {Code}", code);
            return StatusCode(500, new { message = "Error retrieving agents" });
        }
    }

    // GET: api/agents/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AgentDto>> GetAgent(int id)
    {
        try
        {
            var agent = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .Include(a => a.PendingMessages)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
            {
                return NotFound(new { message = $"Agent with id {id} not found" });
            }

            return Ok(MapToDto(agent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent {Id}", id);
            return StatusCode(500, new { message = "Error retrieving agent" });
        }
    }

    // POST: api/agents
    [HttpPost]
    public async Task<ActionResult<AgentDto>> CreateAgent([FromBody] CreateAgentDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Debug logging
            _logger.LogInformation($"[CREATE AGENT] Name: {createDto.Name}, Okpd2Code: {createDto.Okpd2Code}, Parameters count: {createDto.Parameters?.Count ?? 0}");
            if (createDto.Parameters != null && createDto.Parameters.Any())
            {
                foreach (var p in createDto.Parameters)
                {
                    _logger.LogInformation($"  [PARAM] Name: '{p.Name}', Value: '{p.Value}', Unit: '{p.Unit}', Description: '{p.Description}'");
                }
            }

            // Validate that the NodeId exists
            var nodeExists = await _context.TreeNodes.AnyAsync(t => t.Id == createDto.NodeId);
            if (!nodeExists)
            {
                return BadRequest(new { message = $"Tree node with id '{createDto.NodeId}' not found" });
            }

            var hostExists = await _context.Hosts.AnyAsync(h => h.Id == createDto.HostId);
            if (!hostExists)
            {
                return BadRequest(new { message = $"Host with id {createDto.HostId} not found" });
            }

            try
            {
                await _lifecycle.ValidateHostCapacityAsync(createDto.HostId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var agent = new Agent
            {
                NodeId = createDto.NodeId,
                HostId = createDto.HostId,
                Queue = createDto.Queue ?? string.Empty,
                Name = createDto.Name,
                Type = createDto.Type,
                Profession = createDto.Profession,
                Status = createDto.Status,
                Quantity = createDto.Quantity,
                QueueNumber = createDto.QueueNumber,
                Okpd2Code = createDto.Okpd2Code
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            // Add parameters if provided
            if (createDto.Parameters != null && createDto.Parameters.Any())
            {
                foreach (var paramDto in createDto.Parameters)
                {
                    var parameter = new AgentParameter
                    {
                        AgentId = agent.Id,
                        Name = paramDto.Name,
                        Unit = paramDto.Unit,
                        Value = paramDto.Value,
                        Description = paramDto.Description
                    };
                    _context.AgentParameters.Add(parameter);
                }
            }

            await SyncParametersToDictionaryAsync(createDto.Parameters?.Select(p => ((string?)p.Name, p.Description, p.Unit)) ?? Enumerable.Empty<(string?, string?, string?)>());
            await UpsertOkpd2MappingsFromAgentAsync(agent);
            await _context.SaveChangesAsync();

            // Reload agent with parameters + Host, then provision RMQ / host command
            var createdAgent = await _context.Agents
                .Include(a => a.Parameters)
                .Include(a => a.Host)
                .FirstAsync(a => a.Id == agent.Id);

            try
            {
                await _lifecycle.ProvisionAgentAsync(createdAgent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent {Id} created in DB but RMQ/host provision failed", createdAgent.Id);
            }

            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, MapToDto(createdAgent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent");
            return StatusCode(500, new { message = "Error creating agent" });
        }
    }

    // PUT: api/agents/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<AgentDto>> UpdateAgent(int id, [FromBody] UpdateAgentDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agent = await _context.Agents
                .Include(a => a.Parameters)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
            {
                return NotFound(new { message = $"Agent with id {id} not found" });
            }

            if (updateDto.NodeId != null)
                agent.NodeId = updateDto.NodeId;

            if (updateDto.HostId.HasValue)
            {
                var hostOk = await _context.Hosts.AnyAsync(h => h.Id == updateDto.HostId.Value);
                if (!hostOk)
                    return BadRequest(new { message = $"Host {updateDto.HostId} not found" });

                if (updateDto.HostId.Value != agent.HostId)
                {
                    try
                    {
                        await _lifecycle.ValidateHostCapacityAsync(updateDto.HostId.Value);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                }

                agent.HostId = updateDto.HostId.Value;
            }

            if (updateDto.Queue != null)
                agent.Queue = updateDto.Queue;

            if (updateDto.Name != null)
                agent.Name = updateDto.Name;

            if (updateDto.Type != null)
                agent.Type = updateDto.Type;

            if (updateDto.Profession != null)
                agent.Profession = updateDto.Profession;

            if (updateDto.Status.HasValue)
                agent.Status = updateDto.Status.Value;

            if (updateDto.Quantity.HasValue)
                agent.Quantity = updateDto.Quantity;

            if (updateDto.QueueNumber.HasValue)
                agent.QueueNumber = updateDto.QueueNumber;

            // Update OKPD-2 classification fields if provided
            if (updateDto.Okpd2Code != null)
                agent.Okpd2Code = updateDto.Okpd2Code;

            if (updateDto.AgentTypeValue != null)
                agent.AgentTypeValue = updateDto.AgentTypeValue;

            // Update workflow fields if provided
            if (updateDto.WorkflowApplied.HasValue)
                agent.WorkflowApplied = updateDto.WorkflowApplied.Value;

            if (updateDto.WorkflowStatus != null)
                agent.WorkflowStatus = updateDto.WorkflowStatus;

            if (updateDto.AppliedWorkflows != null)
                agent.AppliedWorkflows = System.Text.Json.JsonSerializer.Serialize(updateDto.AppliedWorkflows);

            // Update parameters if provided
            if (updateDto.Parameters != null)
            {
                // Remove all existing parameters
                _context.AgentParameters.RemoveRange(agent.Parameters);

                // Add new parameters
                foreach (var paramDto in updateDto.Parameters)
                {
                    var parameter = new AgentParameter
                    {
                        AgentId = agent.Id,
                        Name = paramDto.Name ?? string.Empty,
                        Unit = paramDto.Unit,
                        Value = paramDto.Value,
                        Description = paramDto.Description
                    };
                    _context.AgentParameters.Add(parameter);
                }
            }

            await SyncParametersToDictionaryAsync(updateDto.Parameters?.Select(p => (p.Name, p.Description, p.Unit)) ?? Enumerable.Empty<(string?, string?, string?)>());
            await UpsertOkpd2MappingsFromAgentAsync(agent);
            await _context.SaveChangesAsync();

            // Reload agent with updated parameters
            agent = await _context.Agents
                .Include(a => a.Parameters)
                .FirstAsync(a => a.Id == id);

            var dto = new AgentDto
            {
                Id = agent.Id,
                NodeId = agent.NodeId,
                Queue = agent.Queue,
                Name = agent.Name,
                Quantity = agent.Quantity,
                QueueNumber = agent.QueueNumber,
                Okpd2Code = agent.Okpd2Code,
                AgentTypeValue = agent.AgentTypeValue,
                Parameters = agent.Parameters.Select(p => new AgentParameterDto
                {
                    Id = p.Id,
                    AgentId = p.AgentId,
                    Name = p.Name,
                    Unit = p.Unit,
                    Value = p.Value,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList(),
                CreatedAt = agent.CreatedAt,
                UpdatedAt = agent.UpdatedAt,
                WorkflowApplied = agent.WorkflowApplied,
                WorkflowStatus = agent.WorkflowStatus,
                AppliedWorkflows = System.Text.Json.JsonSerializer.Deserialize<List<string>>(agent.AppliedWorkflows) ?? new(),
                TechProcessId = agent.TechProcessId
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent {Id}", id);
            return StatusCode(500, new { message = "Error updating agent" });
        }
    }

    // DELETE: api/agents/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgent(int id)
    {
        try
        {
            var agent = await _context.Agents
                .Include(a => a.Parameters)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
            {
                return NotFound(new { message = $"Agent with id {id} not found" });
            }

            await _lifecycle.DeprovisionAgentAsync(agent);

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Agent deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting agent {Id}", id);
            return StatusCode(500, new { message = "Error deleting agent" });
        }
    }

    private static AgentDto MapToDto(Agent a)
    {
        var latestDraft = a.PendingMessages?
            .Where(pm => pm.Status == "draft")
            .OrderByDescending(pm => pm.Id)
            .FirstOrDefault();

        PendingMessageDto? pendingDto = null;
        if (latestDraft != null)
        {
            object payload = new { };
            try
            {
                payload = System.Text.Json.JsonSerializer.Deserialize<object>(latestDraft.Payload) ?? new { };
            }
            catch
            {
                payload = latestDraft.Payload;
            }

            pendingDto = new PendingMessageDto
            {
                Id = latestDraft.Id,
                AgentId = latestDraft.AgentId,
                Kind = latestDraft.Kind,
                WorkflowType = latestDraft.WorkflowType,
                Status = latestDraft.Status,
                CreatedAt = latestDraft.CreatedAt,
                SentAt = latestDraft.SentAt,
                Payload = payload
            };
        }

        return new AgentDto
        {
            Id = a.Id,
            NodeId = a.NodeId,
            HostId = a.HostId,
            HostName = a.Host?.Name,
            Queue = a.Queue,
            Name = a.Name,
            Type = a.Type,
            Profession = a.Profession,
            Status = a.Status,
            Quantity = a.Quantity,
            QueueNumber = a.QueueNumber,
            Okpd2Code = a.Okpd2Code,
            AgentTypeValue = a.AgentTypeValue,
            Parameters = a.Parameters?.Select(p => new AgentParameterDto
            {
                Id = p.Id,
                AgentId = p.AgentId,
                Name = p.Name,
                Unit = p.Unit,
                Value = p.Value,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList() ?? new(),
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            WorkflowApplied = a.WorkflowApplied,
            WorkflowStatus = a.WorkflowStatus,
            AppliedWorkflows = System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.AppliedWorkflows) ?? new(),
            TechProcessId = a.TechProcessId,
            PendingMessage = pendingDto
        };
    }

    private async Task UpsertOkpd2MappingsFromAgentAsync(Agent agent)
    {
        if (string.IsNullOrWhiteSpace(agent.Name) || string.IsNullOrWhiteSpace(agent.Okpd2Code))
        {
            return;
        }

        var keywords = agent.Name.Trim();
        if (keywords.Length > 255)
        {
            keywords = keywords[..255];
        }

        var codes = agent.Okpd2Code
            .Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => !string.IsNullOrWhiteSpace(code) && code != "00.00.00.000")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var code in codes)
        {
            var keywordsLower = keywords.ToLower();
            var exists = await _context.BlankOkpd2Mappings
                .AnyAsync(m => m.Okpd2Code == code && m.Keywords.ToLower() == keywordsLower);

            if (exists)
            {
                continue;
            }

            _context.BlankOkpd2Mappings.Add(new BlankOkpd2Mapping
            {
                Keywords = keywords,
                Okpd2Code = code,
                Description = $"Автоматически добавлено из агента \"{agent.Name}\" ({agent.Type})"
            });
        }
    }

    private async Task SyncParametersToDictionaryAsync(IEnumerable<(string? Name, string? Description, string? Unit)> parameters)
    {
        var distinctParams = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name!.Trim())
            .Select(g => g.First())
            .ToList();

        if (!distinctParams.Any())
        {
            return;
        }

        var names = distinctParams.Select(p => p.Name!.Trim()).ToList();

        var existingNames = await _context.ParameterDictionary
            .Where(p => names.Contains(p.Name))
            .Select(p => p.Name)
            .ToListAsync();

        foreach (var param in distinctParams)
        {
            if (!existingNames.Contains(param.Name!))
            {
                _context.ParameterDictionary.Add(new ParameterDictionary
                {
                    Name = param.Name!,
                    Description = param.Description,
                    Type = "string",
                    Unit = param.Unit
                });
                existingNames.Add(param.Name!);
            }
        }
    }
}
