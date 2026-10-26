namespace Backend.Services.Workflow;

/// <summary>
/// Whitelist разрешенных классов для выполнения в workflow
/// Предотвращает RCE атаки через reflection
/// </summary>
public static class WorkflowFunctionWhitelist
{
    private static readonly HashSet<string> AllowedClasses = new()
    {
        "Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions",
        "Backend.Services.WorkflowFunctions.RabbitMQFunctions",
        "Backend.Services.WorkflowFunctions.AgentInfoFunctions",
        "Backend.Services.WorkflowFunctions.TimeTableFunctions",
        "Backend.Services.WorkflowFunctions.WFListFunctions",
        "Backend.Services.WorkflowFunctions.TechProcessFunctions",
        "Backend.Services.WorkflowFunctions.WorkflowControlFunctions",
        "Backend.Services.WorkflowFunctions.PlanningFunctions",
        // Добавляйте сюда новые разрешенные классы
    };

    public static bool IsAllowed(string className)
    {
        return AllowedClasses.Contains(className);
    }

    public static void AddAllowedClass(string className)
    {
        AllowedClasses.Add(className);
    }

    public static IReadOnlyCollection<string> GetAllowedClasses()
    {
        return AllowedClasses.ToList().AsReadOnly();
    }
}
