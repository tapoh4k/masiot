using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции для работы со списком workflow (wf_list)
/// Категория: WFList
/// </summary>
public class WFListFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WFListFunctions> _logger;

    public WFListFunctions(ApplicationDbContext context, ILogger<WFListFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Создать новый workflow
    /// Параметры: agentId (string), wfType (string), parameters (string - JSON), initiator (string)
    /// Возвращает: wfId (int)
    /// </summary>
    public async Task<int> CreateWorkflow(string agentId, string wfType, string parameters, string initiator)
    {
        _logger.LogInformation("Creating workflow for agent {AgentId}, type {WfType}", agentId, wfType);

        var wf = new WFList
        {
            AgentId = agentId,
            WFType = wfType,
            Parameters = parameters,
            Initiator = initiator,
            Step = 0,
            Status = 0,
            TimeStart = DateTime.UtcNow
        };

        _context.WFLists.Add(wf);
        await _context.SaveChangesAsync();

        return wf.Id;
    }

    /// <summary>
    /// Обновить шаг workflow
    /// Параметры: wfId (int), step (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateWorkflowStep(int wfId, int step)
    {
        _logger.LogInformation("Updating workflow {WfId} to step {Step}", wfId, step);

        var wf = await _context.WFLists.FindAsync(wfId);

        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "false";
        }

        wf.Step = step;
        await _context.SaveChangesAsync();

        return "true";
    }

    /// <summary>
    /// Обновить статус workflow
    /// Параметры: wfId (int), status (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateWorkflowStatus(int wfId, int status)
    {
        _logger.LogInformation("Updating workflow {WfId} status to {Status}", wfId, status);

        var wf = await _context.WFLists.FindAsync(wfId);

        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "false";
        }

        wf.Status = status;
        await _context.SaveChangesAsync();

        return "true";
    }

    /// <summary>
    /// Завершить workflow
    /// Параметры: wfId (int)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> CompleteWorkflow(int wfId)
    {
        _logger.LogInformation("Completing workflow {WfId}", wfId);

        var wf = await _context.WFLists.FindAsync(wfId);

        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "false";
        }

        wf.Status = 1; // Completed (единый код со статусом движка)
        await _context.SaveChangesAsync();

        return "true";
    }

    /// <summary>
    /// Получить информацию о workflow
    /// Параметры: wfId (int)
    /// Возвращает: wfInfo (string) - JSON
    /// </summary>
    public async Task<string> GetWorkflowInfo(int wfId)
    {
        _logger.LogInformation("Getting info for workflow {WfId}", wfId);

        var wf = await _context.WFLists.FindAsync(wfId);

        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "{}";
        }

        var info = new
        {
            wf.Id,
            wf.AgentId,
            wf.WFType,
            wf.Step,
            wf.Status,
            wf.TimeStart,
            wf.Parameters,
            wf.Initiator
        };

        return JsonConvert.SerializeObject(info);
    }

    /// <summary>
    /// Получить активные workflow для агента
    /// Параметры: agentId (string)
    /// Возвращает: workflows (string) - JSON array
    /// </summary>
    public async Task<string> GetActiveWorkflowsForAgent(string agentId)
    {
        _logger.LogInformation("Getting active workflows for agent {AgentId}", agentId);

        var workflows = await _context.WFLists
            .Where(w => w.AgentId == agentId && w.Status < 2)
            .OrderByDescending(w => w.TimeStart)
            .Select(w => new
            {
                w.Id,
                w.WFType,
                w.Step,
                w.Status,
                w.TimeStart,
                w.Initiator
            })
            .ToListAsync();

        return JsonConvert.SerializeObject(workflows);
    }

    /// <summary>
    /// Обновить параметры workflow
    /// Параметры: wfId (int), parameters (string - JSON)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateWorkflowParameters(int wfId, string parameters)
    {
        _logger.LogInformation("Updating parameters for workflow {WfId}", wfId);

        var wf = await _context.WFLists.FindAsync(wfId);

        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "false";
        }

        wf.Parameters = parameters;
        await _context.SaveChangesAsync();

        return "true";
    }
}
