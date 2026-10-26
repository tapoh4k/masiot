using Microsoft.AspNetCore.Mvc;
using Backend.Services.Workflow;
using Backend.Repositories;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly IWFListRepository _wfListRepository;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowExecutionService workflowExecutionService,
        IWFListRepository wfListRepository,
        ILogger<WorkflowController> logger)
    {
        _workflowExecutionService = workflowExecutionService;
        _wfListRepository = wfListRepository;
        _logger = logger;
    }

    /// <summary>
    /// Start a new workflow execution
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var wfId = await _workflowExecutionService.StartWorkflowAsync(
                wfType: dto.WFType,
                agentId: dto.AgentId,
                parameters: dto.Parameters,
                initiator: dto.Initiator
            );

            return Ok(new { wfId, message = "Workflow started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Continue a paused workflow from a specific step
    /// </summary>
    [HttpPost("continue/{id}")]
    public async Task<IActionResult> ContinueWorkflow(int id, [FromBody] ContinueWorkflowDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _workflowExecutionService.ContinueWorkflowAsync(id, dto.FromStep);
            return Ok(new { message = "Workflow continued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing workflow {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Stop a running workflow (status = -2)
    /// </summary>
    [HttpPost("stop/{id}")]
    public async Task<IActionResult> StopWorkflow(int id)
    {
        try
        {
            var workflow = await _wfListRepository.GetByIdAsync(id);
            if (workflow == null)
            {
                return NotFound(new { error = "Workflow not found" });
            }

            // 0 = running, -2 = already stopped (идемпотентно)
            if (workflow.Status != 0 && workflow.Status != -2)
            {
                return BadRequest(new
                {
                    error = $"Workflow {id} нельзя остановить: текущий статус {workflow.Status} (ожидался 0 — running)"
                });
            }

            await _wfListRepository.UpdateStatusAsync(id, -2); // Stopped
            _logger.LogInformation("Workflow {WFId} stopped by API", id);

            return Ok(new { message = "Workflow stopped successfully", wfId = id, status = -2 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping workflow {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkflow(int id)
    {
        try
        {
            var workflow = await _wfListRepository.GetByIdAsync(id);
            if (workflow == null)
            {
                return NotFound(new { error = "Workflow not found" });
            }

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all workflows for a specific agent
    /// </summary>
    [HttpGet("agent/{agentId}")]
    public async Task<IActionResult> GetWorkflowsByAgent(string agentId)
    {
        try
        {
            var workflows = await _wfListRepository.GetByAgentIdAsync(agentId);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows for agent {AgentId}", agentId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get workflows by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetWorkflowsByStatus(int status)
    {
        try
        {
            var workflows = await _wfListRepository.GetByStatusAsync(status);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows by status {Status}", status);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all workflows
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllWorkflows()
    {
        try
        {
            var workflows = await _wfListRepository.GetAllAsync();
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all workflows");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update workflow step
    /// </summary>
    [HttpPatch("{id}/step")]
    public async Task<IActionResult> UpdateWorkflowStep(int id, [FromBody] UpdateStepDto dto)
    {
        try
        {
            await _wfListRepository.UpdateStepAsync(id, dto.Step);
            return Ok(new { message = "Workflow step updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow step {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update workflow status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateWorkflowStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            await _wfListRepository.UpdateStatusAsync(id, dto.Status);
            return Ok(new { message = "Workflow status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow status {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete workflow
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(int id)
    {
        try
        {
            await _wfListRepository.DeleteAsync(id);
            return Ok(new { message = "Workflow deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WFId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
