using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Backend.Services.RabbitMQ;
using Backend.Models.Messages;

namespace Backend.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IRabbitMQService rabbitMQService, ILogger<MessagesController> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    // POST: api/messages/work
    [HttpPost("work")]
    public IActionResult SendWorkMessage([FromBody] WorkMessage message)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _rabbitMQService.SendMessage(message, message.ReceiverId, message.MessageType);
            _logger.LogInformation("Work message sent to {ReceiverId}", message.ReceiverId);
            return Ok(new { message = "Work message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending work message");
            return StatusCode(500, new { message = "Error sending work message" });
        }
    }

    // POST: api/messages/agent-info
    [HttpPost("agent-info")]
    public IActionResult SendAgentInfoMessage([FromBody] AgentInfoMessage message)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _rabbitMQService.SendMessage(message, message.ReceiverId, "3-6-1-2");
            _logger.LogInformation("Agent info message sent to {ReceiverId}", message.ReceiverId);
            return Ok(new { message = "Agent info message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent info message");
            return StatusCode(500, new { message = "Error sending agent info message" });
        }
    }

    // POST: api/messages/new-workflow
    [HttpPost("new-workflow")]
    public IActionResult SendNewWorkflowMessage([FromBody] NewWFMessage message)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _rabbitMQService.SendMessage(message, message.ReceiverId, "3-6-1-1");
            _logger.LogInformation("New workflow message sent to {ReceiverId}", message.ReceiverId);
            return Ok(new { message = "New workflow message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new workflow message");
            return StatusCode(500, new { message = "Error sending new workflow message" });
        }
    }

    // POST: api/messages/send
    [HttpPost("send")]
    public IActionResult SendGenericMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _rabbitMQService.SendMessage(request.Message, request.RoutingKey, request.MessageType);
            _logger.LogInformation("Generic message sent to {RoutingKey}", request.RoutingKey);
            return Ok(new { message = "Message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending generic message");
            return StatusCode(500, new { message = "Error sending message" });
        }
    }
}

public class SendMessageRequest
{
    [Required(ErrorMessage = "Сообщение обязательно")]
    public object Message { get; set; } = new();

    [Required(ErrorMessage = "Routing key обязателен")]
    [StringLength(255, ErrorMessage = "Routing key не может превышать 255 символов")]
    public string RoutingKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Тип сообщения обязателен")]
    [StringLength(100, ErrorMessage = "Тип сообщения не может превышать 100 символов")]
    public string MessageType { get; set; } = string.Empty;
}
