using Backend.Models;

namespace Backend.Services.Workflow;

public interface IWorkflowExecutionService
{
    Task<int> StartWorkflowAsync(string wfType, string agentId, Dictionary<string, object> parameters, string? initiator = null);
    Task ContinueWorkflowAsync(int wfId, int fromStep, Dictionary<string, object>? additionalParameters = null);
    Task<WFList?> GetWorkflowStatusAsync(int wfId);
    Task<List<WFList>> GetAgentWorkflowsAsync(string agentId);
}
