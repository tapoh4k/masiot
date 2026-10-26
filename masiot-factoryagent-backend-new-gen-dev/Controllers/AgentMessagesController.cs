using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;
using Backend.Services.RabbitMQ;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentMessagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRabbitMQService _rabbitMqService;

    public AgentMessagesController(ApplicationDbContext context, IRabbitMQService rabbitMqService)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> PostMessageAsync(int agentId)
    {
        var agent = await _context.Agents
            .Include(a => a.Parameters)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agent == null)
        {
            return NotFound($"Agent with ID {agentId} not found.");
        }

        var agentKey = AgentRoutingHelper.BuildAgentId(agent.Id);
        var parameters = agent.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .ToDictionary(p => p.Name, p => (object?)(p.Value ?? string.Empty));

        var message = new
        {
            parameter = new
            {
                agentId = agentKey,
                status = agent.Status,
                profession = agent.Profession ?? string.Empty,
                quantity = agent.Quantity ?? 0,
                parameters
            },
            messageType = "3-6-1-2",
            senderId = "fabric",
            receiverId = agentKey
        };

        var routingKeys = AgentRoutingHelper.GetAgentRoutingKeys(agent);
        await _rabbitMqService.EnsureQueueWithBindingsAsync(agentKey, routingKeys);
        _rabbitMqService.SendMessage(message, agentKey, "3-6-1-2");

        return Ok(new { message = "Message sent successfully.", agentId = agentKey, routingKeys });
    }
}
