using Backend.Models;
using Backend.Models.Messages;
using Backend.Repositories;
using Backend.Data;
using Backend.DTOs;
using Backend.Services.RabbitMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Workflow;

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IWFListRepository _wfListRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowExecutionService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IParameterValidationService _validationService;
    private readonly IRabbitMQService _rabbitMQService;

    public WorkflowExecutionService(
        IWFListRepository wfListRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowExecutionService> logger,
        ApplicationDbContext context,
        IParameterValidationService validationService,
        IRabbitMQService rabbitMQService)
    {
        _wfListRepository = wfListRepository;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _context = context;
        _validationService = validationService;
        _rabbitMQService = rabbitMQService;
    }

    public async Task<int> StartWorkflowAsync(string wfType, string agentId, Dictionary<string, object> parameters, string? initiator = null)
    {
        try
        {
            // Load workflow template from database
            var template = await _context.WorkflowTemplates
                .FirstOrDefaultAsync(wt => wt.Name == wfType);

            string wfFunctions = "[]";

            // Normalize aliases (tech_process_id → techProcessId) and unwrap JsonElement/JToken
            parameters = WorkflowParameterNormalizer.Normalize(parameters);
            ApplyWorkflowStartDefaults(parameters);

            if (template != null)
            {
                // Validate parameters before starting workflow
                var validationErrors = await ValidateWorkflowParametersAsync(template.Nodes, parameters);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("Parameter validation failed for workflow {WFType}: {Errors}",
                        wfType, string.Join(", ", validationErrors));

                    // Send error message via RabbitMQ
                    SendValidationErrorMessageAsync(agentId, wfType, validationErrors);

                    throw new InvalidOperationException($"Ошибка валидации параметров: {string.Join(", ", validationErrors)}");
                }

                // Convert React Flow format to execution format
                wfFunctions = WorkflowFormatConverter.ConvertReactFlowToFunctions(
                    template.Nodes,
                    template.Edges
                );
                _logger.LogInformation("Loaded workflow template {WFType} from database", wfType);
            }
            else
            {
                _logger.LogWarning("Workflow template {WFType} not found in database", wfType);
            }

            var wfList = new WFList
            {
                Step = 0,
                Status = 0, // Running
                Parameters = JsonConvert.SerializeObject(parameters),
                AgentId = agentId,
                WFType = wfType,
                WFFunctions = wfFunctions,
                Initiator = initiator,
                TimeStart = DateTime.UtcNow
            };

            var wfId = await _wfListRepository.AddAsync(wfList);

            // Движок должен подставлять реальный wfId — иначе CompleteWorkflow / Sleep / SPR ломаются
            parameters["wfId"] = wfId;
            await _wfListRepository.UpdateParametersAsync(wfId, JsonConvert.SerializeObject(parameters));

            _logger.LogInformation("Started workflow {WFType} for agent {AgentId} with ID {WFId}", wfType, agentId, wfId);

            // Start execution in background with a NEW root scope (IServiceScopeFactory —
            // never use request-scoped IServiceProvider: RabbitMQ/HTTP scope may already be disposed)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedWfListRepository = scope.ServiceProvider.GetRequiredService<IWFListRepository>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WorkflowExecutionService>>();
                    await ExecuteWorkflowInScopeAsync(wfId, 0, scopedWfListRepository, scopedLogger, scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background execution failed for workflow {WFId}", wfId);
                }
            });

            return wfId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow {WFType} for agent {AgentId}", wfType, agentId);
            throw;
        }
    }

    public async Task ContinueWorkflowAsync(int wfId, int fromStep, Dictionary<string, object>? additionalParameters = null)
    {
        try
        {
            var wfList = await _wfListRepository.GetByIdAsync(wfId);
            if (wfList == null)
            {
                _logger.LogWarning("Workflow {WFId} not found", wfId);
                return;
            }

            // Merge additional parameters
            var currentParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(wfList.Parameters) ?? new();
            if (additionalParameters != null)
            {
                foreach (var kvp in additionalParameters)
                {
                    currentParams[kvp.Key] = kvp.Value;
                }
                await _wfListRepository.UpdateParametersAsync(wfId, JsonConvert.SerializeObject(currentParams));
            }

            // Continue execution in background with a NEW root scope
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedWfListRepository = scope.ServiceProvider.GetRequiredService<IWFListRepository>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WorkflowExecutionService>>();
                    await ExecuteWorkflowInScopeAsync(wfId, fromStep, scopedWfListRepository, scopedLogger, scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background continue failed for workflow {WFId}", wfId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing workflow {WFId}", wfId);
            throw;
        }
    }

    private async Task ExecuteWorkflowAsync(int wfId, int startStep = 0)
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedWfListRepository = scope.ServiceProvider.GetRequiredService<IWFListRepository>();
        var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WorkflowExecutionService>>();
        await ExecuteWorkflowInScopeAsync(wfId, startStep, scopedWfListRepository, scopedLogger, scope.ServiceProvider);
    }

    private async Task ExecuteWorkflowInScopeAsync(int wfId, int startStep, IWFListRepository wfListRepository, ILogger<WorkflowExecutionService> logger, IServiceProvider scopedServiceProvider)
    {
        try
        {
            var wfList = await wfListRepository.GetByIdAsync(wfId);
            if (wfList == null)
            {
                logger.LogWarning("Workflow {WFId} not found during execution", wfId);
                return;
            }

            var functions = JsonConvert.DeserializeObject<List<WFFunction>>(wfList.WFFunctions) ?? new();
            var parameters = WorkflowParameterNormalizer.Normalize(
                JsonConvert.DeserializeObject<Dictionary<string, object>>(wfList.Parameters),
                wfId: wfId);

            logger.LogInformation("Executing workflow {WFId} from step {Step}, total steps: {TotalSteps}",
                wfId, startStep, functions.Count);

            for (int i = startStep; i < functions.Count; i++)
            {
                // Позволяем остановку между шагами (status -2)
                var latest = await wfListRepository.GetByIdAsync(wfId);
                if (latest == null)
                {
                    logger.LogWarning("Workflow {WFId} disappeared during execution", wfId);
                    return;
                }
                if (latest.Status == -2)
                {
                    logger.LogInformation("Workflow {WFId} stopped at step {Step}", wfId, i);
                    return;
                }

                var function = functions[i];

                try
                {
                    logger.LogInformation("Executing step {Step}: {FunctionClass}.{FunctionName}",
                        i, function.FunctionClass, function.Name);

                    // Security: Check if class is in whitelist
                    if (!WorkflowFunctionWhitelist.IsAllowed(function.FunctionClass))
                    {
                        logger.LogError("Class {ClassName} is not in whitelist - security violation", function.FunctionClass);
                        await wfListRepository.UpdateStatusAsync(wfId, -1);
                        return;
                    }

                    // Get the class type
                    var classType = Type.GetType(function.FunctionClass);
                    if (classType == null)
                    {
                        logger.LogError("Class {ClassName} not found", function.FunctionClass);
                        await wfListRepository.UpdateStatusAsync(wfId, -1); // Error status
                        return;
                    }

                    // Get instance from DI container
                    var classInstance = scopedServiceProvider.GetService(classType);
                    if (classInstance == null)
                    {
                        logger.LogError("Could not create instance of {ClassName}", function.FunctionClass);
                        await wfListRepository.UpdateStatusAsync(wfId, -1);
                        return;
                    }

                    // Get the method
                    var method = classType.GetMethod(function.Name);
                    if (method == null)
                    {
                        logger.LogError("Method {MethodName} not found in class {ClassName}",
                            function.Name, function.FunctionClass);
                        await wfListRepository.UpdateStatusAsync(wfId, -1);
                        return;
                    }

                    // Prepare method parameters
                    var methodParameters = method.GetParameters();
                    if (methodParameters.Length != function.NeedParam.Count)
                    {
                        logger.LogError("Method {MethodName} expects {Expected} parameters, workflow provides {Actual}",
                            function.Name, methodParameters.Length, function.NeedParam.Count);
                        await wfListRepository.UpdateStatusAsync(wfId, -1);
                        return;
                    }

                    var methodParams = new object?[methodParameters.Length];
                    for (int p = 0; p < methodParameters.Length; p++)
                    {
                        var paramName = function.NeedParam[p];
                        if (parameters.TryGetValue(paramName, out var paramValue))
                        {
                            methodParams[p] = ConvertArgument(paramValue, methodParameters[p].ParameterType);
                        }
                        else
                        {
                            logger.LogWarning("Parameter {ParamName} not found in workflow parameters", paramName);
                        }
                    }

                    // Invoke the method
                    var result = method.Invoke(classInstance, methodParams);
                    if (result is Task task)
                    {
                        await task;
                        result = method.ReturnType.IsGenericType
                            ? method.ReturnType.GetProperty(nameof(Task<object>.Result))!.GetValue(task)
                            : null;
                    }

                    // Store return value if specified
                    if (!string.IsNullOrEmpty(function.ParamReturn) && result != null)
                    {
                        parameters[function.ParamReturn] = result;
                        PromoteWorkflowAliases(parameters, function.ParamReturn, result);
                    }

                    // Update workflow state
                    await wfListRepository.UpdateStepAsync(wfId, i);
                    await wfListRepository.UpdateParametersAsync(wfId, JsonConvert.SerializeObject(parameters));

                    logger.LogInformation("Step {Step} completed successfully", i);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing step {Step} of workflow {WFId}", i, wfId);
                    await wfListRepository.UpdateStatusAsync(wfId, -1); // Error status
                    return;
                }
            }

            // Mark workflow as completed
            await wfListRepository.UpdateStatusAsync(wfId, 1); // Completed status
            logger.LogInformation("Workflow {WFId} completed successfully", wfId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error executing workflow {WFId}", wfId);
            try
            {
                await wfListRepository.UpdateStatusAsync(wfId, -1);
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Error updating workflow status after failure");
            }
        }
    }

    // Параметры workflow хранятся в JSON и после Newtonsoft приходят как long, double,
    // DateTime, string или JToken — приводим их к типу параметра вызываемого метода.
    private static object? ConvertArgument(object? value, Type targetType)
    {
        if (value == null)
        {
            var underlyingNull = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (underlyingNull == typeof(string)) return string.Empty;
            if (underlyingNull.IsValueType) return Activator.CreateInstance(underlyingNull);
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value)) return value;

        if (underlying == typeof(string))
        {
            return value switch
            {
                JValue { Type: JTokenType.String } jString => jString.ToString(),
                JToken token => token.ToString(Formatting.None),
                DateTime dt => dt.ToString("o"),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        if (value is JValue jValue) value = jValue.Value!;
        return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Проброс результатов шага в имена параметров следующих шагов
    /// (nextOperation → opNo, message JSON → messageType/routingKey/payload).
    /// </summary>
    private static void PromoteWorkflowAliases(Dictionary<string, object> parameters, string paramReturn, object result)
    {
        if (string.Equals(paramReturn, "nextOperation", StringComparison.OrdinalIgnoreCase))
        {
            var op = WorkflowParameterNormalizer.AsInt(result);
            if (op.HasValue)
            {
                parameters["opNo"] = op.Value;
                parameters["operationNumber"] = op.Value;
                parameters["currentOpNo"] = op.Value;
            }
        }

        if (string.Equals(paramReturn, "message", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var json = result as string ?? JsonConvert.SerializeObject(result);
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (obj == null) return;

                if (obj.TryGetValue("messageType", out var mt) && mt != null)
                    parameters["messageType"] = WorkflowParameterNormalizer.Unwrap(mt)?.ToString() ?? "";
                if (obj.TryGetValue("routingKey", out var rk) && rk != null)
                    parameters["routingKey"] = WorkflowParameterNormalizer.Unwrap(rk)?.ToString() ?? "";
                if (obj.TryGetValue("payload", out var pl) && pl != null)
                {
                    parameters["payload"] = pl is string s
                        ? s
                        : JsonConvert.SerializeObject(WorkflowParameterNormalizer.Unwrap(pl));
                }
            }
            catch
            {
                // leave raw message as-is
            }
        }
    }

    private static void ApplyWorkflowStartDefaults(Dictionary<string, object> parameters)
    {
        void SetDefault(string key, object value)
        {
            if (!parameters.ContainsKey(key) || parameters[key] == null ||
                (parameters[key] is string s && string.IsNullOrWhiteSpace(s)))
            {
                parameters[key] = value;
            }
        }

        SetDefault("delaySeconds", 0);
        SetDefault("workMessageType", "work");
        SetDefault("typePlan", "auto");
        SetDefault("result", true);
        SetDefault("shiftTime", 0);
        SetDefault("deltaTime", 0);
        SetDefault("messages", "[]");
        SetDefault("payload", "{}");
        SetDefault("messageType", "work");
        SetDefault("routingKey", "backend");
        SetDefault("orderNumber", $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}");
        SetDefault("timeEnd", DateTime.UtcNow.ToString("o"));

        if (parameters.TryGetValue("techProcessId", out var tp) &&
            WorkflowParameterNormalizer.AsInt(tp) is int tpId)
        {
            SetDefault("opNo", 0); // TechprocessPrepare перезапишет через nextOperation
            _ = tpId;
        }
    }

    public async Task<WFList?> GetWorkflowStatusAsync(int wfId)
    {
        return await _wfListRepository.GetByIdAsync(wfId);
    }

    public async Task<List<WFList>> GetAgentWorkflowsAsync(string agentId)
    {
        return await _wfListRepository.GetByAgentIdAsync(agentId);
    }

    private async Task<List<string>> ValidateWorkflowParametersAsync(string nodesJson, Dictionary<string, object> parameters)
    {
        var errors = new List<string>();

        try
        {
            var nodes = JsonConvert.DeserializeObject<List<dynamic>>(nodesJson);
            if (nodes == null) return errors;

            foreach (var node in nodes)
            {
                string nodeType = node.type?.ToString() ?? "";
                if (nodeType != "functionNode") continue;

                var nodeData = node.data;
                if (nodeData == null) continue;

                int? functionId = nodeData.functionId;
                if (functionId == null) continue;

                var workflowFunction = await _context.WorkflowFunctions.FindAsync((int)functionId);
                if (workflowFunction == null) continue;

                var paramDefs = JsonConvert.DeserializeObject<List<WorkflowParameterDto>>(workflowFunction.Params);
                if (paramDefs == null || paramDefs.Count == 0) continue;

                var validationResult = _validationService.ValidateParameters(paramDefs, parameters);
                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow parameters");
            errors.Add("Ошибка при валидации параметров");
        }

        return errors;
    }

    private void SendValidationErrorMessageAsync(string agentId, string wfType, List<string> errors)
    {
        try
        {
            var errorMessage = new
            {
                type = "validation_error",
                workflow = wfType,
                agent_id = agentId,
                errors = errors,
                timestamp = DateTime.UtcNow
            };

            _rabbitMQService.SendMessage(errorMessage, $"agent.{agentId}.error", "validation_error");

            _logger.LogInformation("Sent validation error message to agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending validation error message");
        }
    }
}
