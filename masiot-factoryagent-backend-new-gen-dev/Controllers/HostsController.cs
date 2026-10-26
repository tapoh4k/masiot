using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Backend.Services.Hosts;

namespace Backend.Controllers;

[ApiController]
[Route("api/hosts")]
public class HostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IHostProvisioningService _provisioning;
    private readonly IHostAgentCommandDispatcher _commands;
    private readonly ILogger<HostsController> _logger;

    public HostsController(
        ApplicationDbContext context,
        IEncryptionService encryption,
        IHostProvisioningService provisioning,
        IHostAgentCommandDispatcher commands,
        ILogger<HostsController> logger)
    {
        _context = context;
        _encryption = encryption;
        _provisioning = provisioning;
        _commands = commands;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HostDto>>> GetAll()
    {
        var hosts = await _context.Hosts
            .Select(h => new HostDto
            {
                Id = h.Id,
                Name = h.Name,
                Type = h.Type,
                Status = h.Status,
                MaxAgents = h.MaxAgents,
                DeployTarget = h.DeployTarget,
                RmqUsername = h.RmqUsername,
                QueueName = h.QueueName,
                RoutingKey = h.RoutingKey,
                AgentsCount = h.Agents.Count,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt
            })
            .OrderBy(h => h.Id)
            .ToListAsync();

        return Ok(hosts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HostDto>> GetById(int id)
    {
        var host = await _context.Hosts
            .Where(h => h.Id == id)
            .Select(h => new HostDto
            {
                Id = h.Id,
                Name = h.Name,
                Type = h.Type,
                Status = h.Status,
                MaxAgents = h.MaxAgents,
                DeployTarget = h.DeployTarget,
                RmqUsername = h.RmqUsername,
                QueueName = h.QueueName,
                RoutingKey = h.RoutingKey,
                AgentsCount = h.Agents.Count,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (host == null)
            return NotFound(new { message = $"Host {id} not found" });

        return Ok(host);
    }

    [HttpPost]
    public async Task<ActionResult<HostDto>> Create([FromBody] CreateHostDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var type = dto.Type.Trim().ToLowerInvariant();
        var maxAgents = type == HostTypes.Single
            ? 1
            : (dto.MaxAgents ?? (type == HostTypes.Cluster ? 100 : 50));

        var deploy = string.IsNullOrWhiteSpace(dto.DeployTarget)
            ? (type == HostTypes.Specific ? HostDeployTargets.Custom : HostDeployTargets.Docker)
            : dto.DeployTarget!.Trim().ToLowerInvariant();

        if (type == HostTypes.Single && deploy == HostDeployTargets.Custom)
            deploy = HostDeployTargets.Docker;

        var host = new AgentHost
        {
            Name = dto.Name.Trim(),
            Type = type,
            Status = "offline",
            MaxAgents = maxAgents,
            DeployTarget = deploy
        };

        _context.Hosts.Add(host);
        await _context.SaveChangesAsync();

        host.QueueName = AgentRoutingHelper.BuildHostQueueName(host.Id);
        host.RoutingKey = AgentRoutingHelper.BuildHostRoutingKey(host.Id);
        host.RmqUsername = _provisioning.GenerateUsername(host.Id);
        var plainPassword = _provisioning.GeneratePassword();
        host.RmqPasswordEnc = _encryption.Encrypt(plainPassword);

        try
        {
            await _provisioning.ProvisionRabbitMqAsync(host, plainPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Host RMQ provision failed for {HostId}", host.Id);
            // Host остаётся в БД — очередь можно допровиженить позже
        }

        await _context.SaveChangesAsync();

        var result = Map(host, 0);
        return CreatedAtAction(nameof(GetById), new { id = host.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<HostDto>> Update(int id, [FromBody] UpdateHostDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var host = await _context.Hosts.Include(h => h.Agents).FirstOrDefaultAsync(h => h.Id == id);
        if (host == null)
            return NotFound(new { message = $"Host {id} not found" });

        if (dto.Name != null)
            host.Name = dto.Name.Trim();

        if (dto.Status != null)
            host.Status = dto.Status;

        if (dto.DeployTarget != null)
            host.DeployTarget = dto.DeployTarget;

        if (dto.MaxAgents.HasValue)
        {
            if (host.Type == HostTypes.Single && dto.MaxAgents.Value != 1)
                return BadRequest(new { message = "Single host всегда max_agents=1" });

            if (dto.MaxAgents.Value < host.Agents.Count)
                return BadRequest(new { message = $"max_agents не может быть меньше текущего числа агентов ({host.Agents.Count})" });

            host.MaxAgents = dto.MaxAgents.Value;
        }

        await _context.SaveChangesAsync();
        return Ok(Map(host, host.Agents.Count));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var host = await _context.Hosts.Include(h => h.Agents).FirstOrDefaultAsync(h => h.Id == id);
        if (host == null)
            return NotFound(new { message = $"Host {id} not found" });

        if (host.Agents.Any())
            return BadRequest(new { message = "Нельзя удалить Host с привязанными агентами. Сначала переместите или удалите агентов." });

        await _provisioning.DeprovisionRabbitMqAsync(host);
        _context.Hosts.Remove(host);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Host deleted" });
    }

    /// <summary>Скелет команд Host 1–13 (публикация в очередь Host).</summary>
    [HttpPost("{id:int}/commands/{command}")]
    public async Task<IActionResult> SendCommand(int id, string command, [FromBody] HostCommandRequest? body)
    {
        var host = await _context.Hosts.FirstOrDefaultAsync(h => h.Id == id);
        if (host == null)
            return NotFound(new { message = $"Host {id} not found" });

        var normalized = NormalizeCommand(command);
        if (normalized == null)
            return BadRequest(new { message = "Неизвестная команда", allowed = AllowedCommands });

        var agentIds = body?.AgentIds ?? new List<string>();
        await _commands.SendAsync(host, normalized, agentIds, body?.Payload);
        return Ok(new { message = "Command published", command = normalized, hostId = id });
    }

    private static readonly string[] AllowedCommands =
    {
        HostAgentCommandTypes.CreateAgent,
        HostAgentCommandTypes.CreateAgents,
        HostAgentCommandTypes.StartAgent,
        HostAgentCommandTypes.StartAgents,
        HostAgentCommandTypes.StartAllAgents,
        HostAgentCommandTypes.StopAgent,
        HostAgentCommandTypes.StopAgents,
        HostAgentCommandTypes.StopAllAgents,
        HostAgentCommandTypes.DeleteAgent,
        HostAgentCommandTypes.DeleteAgents,
        HostAgentCommandTypes.DeleteAllAgents,
        HostAgentCommandTypes.MoveAgent,
        HostAgentCommandTypes.MoveAgents
    };

    private static string? NormalizeCommand(string command)
    {
        var c = command.Trim().ToLowerInvariant().Replace('-', '_');
        if (!c.StartsWith("host."))
            c = "host." + c;

        return AllowedCommands.FirstOrDefault(a => a.Equals(c, StringComparison.OrdinalIgnoreCase));
    }

    private static HostDto Map(AgentHost h, int agentsCount) => new()
    {
        Id = h.Id,
        Name = h.Name,
        Type = h.Type,
        Status = h.Status,
        MaxAgents = h.MaxAgents,
        DeployTarget = h.DeployTarget,
        RmqUsername = h.RmqUsername,
        QueueName = h.QueueName,
        RoutingKey = h.RoutingKey,
        AgentsCount = agentsCount,
        CreatedAt = h.CreatedAt,
        UpdatedAt = h.UpdatedAt
    };
}

public class HostCommandRequest
{
    public List<string> AgentIds { get; set; } = new();
    public Dictionary<string, object?>? Payload { get; set; }
}
