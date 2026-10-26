using Backend.Models.Messages;
using Backend.Services.Workflow;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.RabbitMQ;

public class MessageProcessingService : IMessageProcessingService
{
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageProcessingService> _logger;

    public MessageProcessingService(
        IWorkflowExecutionService workflowExecutionService,
        ApplicationDbContext context,
        ILogger<MessageProcessingService> logger)
    {
        _workflowExecutionService = workflowExecutionService;
        _context = context;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(string messageJson, string messageType)
    {
        try
        {
            _logger.LogInformation("Processing message of type {MessageType}", messageType);

            switch (messageType)
            {
                case "3-6-1-1": // New Workflow Definition
                    await ProcessNewWorkflowMessageAsync(messageJson);
                    break;

                case "3-6-1-2": // Agent Info Update
                    await ProcessAgentInfoMessageAsync(messageJson);
                    break;

                default: // Work Message - trigger workflow execution
                    await ProcessWorkMessageAsync(messageJson, messageType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message of type {MessageType}", messageType);
        }
    }

    private async Task ProcessNewWorkflowMessageAsync(string messageJson)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<NewWFMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize NewWFMessage");
                return;
            }

            _logger.LogInformation("Received new workflow definition: {WFType}", message.WFType);

            // Store workflow template in database
            var workflowTemplate = new Models.WorkflowTemplate
            {
                Name = message.WFType,
                Description = $"Workflow {message.WFType}",
                Nodes = JsonConvert.SerializeObject(message.WFFunctions),
                Edges = "[]"
            };

            _context.WorkflowTemplates.Add(workflowTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Workflow template {WFType} saved successfully", message.WFType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new workflow message");
        }
    }

    private async Task ProcessAgentInfoMessageAsync(string messageJson)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<AgentInfoMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize AgentInfoMessage");
                return;
            }

            _logger.LogInformation("Received agent info update for agent {AgentId}", message.SenderId);

            // Log the message content if present
            if (message.Parameter.TryGetValue("message", out var messageObj) && messageObj != null)
            {
                _logger.LogInformation("Agent Message: {Message}", messageObj.ToString());
                Console.WriteLine($"[AGENT INFO] {messageObj}");
            }

            // Extract agent info from parameters
            if (message.Parameter.TryGetValue("agentId", out var agentIdObj) && agentIdObj is string agentId)
            {
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.NodeId == agentId);
                if (agent != null)
                {
                    // Update agent info
                    if (message.Parameter.TryGetValue("profession", out var professionObj))
                        agent.Profession = professionObj?.ToString();

                    if (message.Parameter.TryGetValue("status", out var statusObj) && statusObj != null)
                        agent.Status = Convert.ToInt32(statusObj);

                    if (message.Parameter.TryGetValue("quantity", out var quantityObj) && quantityObj != null)
                        agent.Quantity = Convert.ToInt32(quantityObj);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Agent {AgentId} info updated", agentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent info message");
        }
    }

    private async Task ProcessWorkMessageAsync(string messageJson, string messageType)
    {
        try
        {
            // Backend слушает # — служебные сообщения ресурсам (work/spr/toSPR/...) не должны
            // поднимать workflow. Стартуем только если есть шаблон с таким именем.
            var templateExists = await _context.WorkflowTemplates
                .AsNoTracking()
                .AnyAsync(t => t.Name == messageType);
            if (!templateExists)
            {
                _logger.LogInformation(
                    "Ignoring message type {MessageType} — no matching workflow template",
                    messageType);
                return;
            }

            var message = JsonConvert.DeserializeObject<WorkMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize WorkMessage");
                return;
            }

            _logger.LogInformation("Received work message from {SenderId} to {ReceiverId}, type: {MessageType}",
                message.SenderId, message.ReceiverId, messageType);

            var parameters = Backend.Services.Workflow.WorkflowParameterNormalizer.Normalize(message.Parameter);

            // Start workflow execution for the receiving agent
            var wfId = await _workflowExecutionService.StartWorkflowAsync(
                wfType: messageType,
                agentId: message.ReceiverId,
                parameters: parameters,
                initiator: message.SenderId
            );

            _logger.LogInformation("Started workflow {WFId} for agent {AgentId}", wfId, message.ReceiverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing work message");
        }
    }
}
