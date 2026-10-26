using Backend.Data;
using Backend.Models;
using Backend.Services.RabbitMQ;
using Backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Функции управления workflow для процесса планирования
/// Категория: WorkflowControl
/// </summary>
public class WorkflowControlFunctions
{
    private readonly ApplicationDbContext _context;
    private readonly IWFListRepository _wfListRepository;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<WorkflowControlFunctions> _logger;

    public WorkflowControlFunctions(
        ApplicationDbContext context,
        IWFListRepository wfListRepository,
        IRabbitMQService rabbitMQService,
        ILogger<WorkflowControlFunctions> logger)
    {
        _context = context;
        _wfListRepository = wfListRepository;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    /// <summary>
    /// Приостановить выполнение workflow на указанное время
    /// Параметры: wfId (int), delaySeconds (int)
    /// Возвращает: status (string) - "paused"
    /// </summary>
    public async Task<string> WorkFlowSleep(int wfId, int delaySeconds)
    {
        _logger.LogInformation("Pausing workflow {WfId} for {DelaySeconds} seconds", wfId, delaySeconds);

        await Task.Delay(delaySeconds * 1000);

        return "paused";
    }

    /// <summary>
    /// Остановить выполнение workflow
    /// Параметры: wfId (int)
    /// Возвращает: status (string) - "stopped"
    /// </summary>
    public async Task<string> WorkFlowStop(int wfId)
    {
        _logger.LogInformation("Stopping workflow {WfId}", wfId);

        await _wfListRepository.UpdateStatusAsync(wfId, -2); // Stopped status

        return "stopped";
    }

    /// <summary>
    /// Продолжить выполнение приостановленного workflow
    /// Параметры: wfId (int), fromStep (int)
    /// Возвращает: status (string) - "continued"
    /// </summary>
    public async Task<string> WorkFlowContinue(int wfId, int fromStep)
    {
        _logger.LogInformation("Continuing workflow {WfId} from step {FromStep}", wfId, fromStep);

        var wf = await _wfListRepository.GetByIdAsync(wfId);
        if (wf == null)
        {
            _logger.LogWarning("Workflow {WfId} not found", wfId);
            return "not_found";
        }

        // Update status to running
        await _wfListRepository.UpdateStatusAsync(wfId, 0);
        await _wfListRepository.UpdateStepAsync(wfId, fromStep);

        return "continued";
    }

    /// <summary>
    /// Запустить workflow по условию
    /// Параметры: condition (bool), firstRoute (string), secondRoute (string), parameters (string - JSON)
    /// Возвращает: route (string) - выбранный маршрут
    /// </summary>
    public string WorkFlowRunByOneCondition(bool condition, string firstRoute, string secondRoute, string parameters)
    {
        _logger.LogInformation("Running workflow by condition: {Condition}, routes: {FirstRoute}/{SecondRoute}",
            condition, firstRoute, secondRoute);

        var selectedRoute = condition ? firstRoute : secondRoute;

        // Parse parameters and send message to start the selected workflow
        var paramsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters) ?? new();

        // In real implementation, this would trigger the next workflow
        // For now, just return the selected route
        return selectedRoute;
    }

    /// <summary>
    /// Отправить сообщение
    /// Параметры: messageType (string), routingKey (string), payload (string - JSON)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string SendMessage(string messageType, string routingKey, string payload)
    {
        _logger.LogInformation("Sending message type {MessageType} to {RoutingKey}", messageType, routingKey);

        var message = JsonConvert.DeserializeObject<object>(payload);
        _rabbitMQService.SendMessage(message!, routingKey, messageType);

        return "sent";
    }

    /// <summary>
    /// Отправить несколько подготовленных сообщений
    /// Параметры: messages (string - JSON array)
    /// Возвращает: count (int) - количество отправленных сообщений
    /// </summary>
    public int SendReadyMessages(string messages)
    {
        _logger.LogInformation("Sending ready messages");

        var messageList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(messages);
        if (messageList == null) return 0;

        int count = 0;
        foreach (var msg in messageList)
        {
            if (msg.TryGetValue("messageType", out var typeObj) &&
                msg.TryGetValue("routingKey", out var keyObj) &&
                msg.TryGetValue("payload", out var payloadObj))
            {
                var messageType = typeObj.ToString() ?? "";
                var routingKey = keyObj.ToString() ?? "";

                _rabbitMQService.SendMessage(payloadObj, routingKey, messageType);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Получить текущий шаг workflow
    /// Параметры: wfId (int)
    /// Возвращает: step (int)
    /// </summary>
    public async Task<int> GetWorkflowStep(int wfId)
    {
        var wf = await _wfListRepository.GetByIdAsync(wfId);
        return wf?.Step ?? -1;
    }

    /// <summary>
    /// Получить параметры workflow
    /// Параметры: wfId (int)
    /// Возвращает: parameters (string) - JSON
    /// </summary>
    public async Task<string> GetWorkflowParameters(int wfId)
    {
        var wf = await _wfListRepository.GetByIdAsync(wfId);
        return wf?.Parameters ?? "{}";
    }

    /// <summary>
    /// Обновить параметры workflow
    /// Параметры: wfId (int), parameters (string - JSON)
    /// Возвращает: success (string) - "true" или "false"
    /// </summary>
    public async Task<string> UpdateWorkflowParameters(int wfId, string parameters)
    {
        _logger.LogInformation("Updating parameters for workflow {WfId}", wfId);

        await _wfListRepository.UpdateParametersAsync(wfId, parameters);

        return "true";
    }
}
