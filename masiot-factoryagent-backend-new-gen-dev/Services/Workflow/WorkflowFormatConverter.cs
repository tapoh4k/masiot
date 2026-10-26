using System.Text.Json;

namespace Backend.Services.Workflow;

public class WorkflowFormatConverter
{
    public static string ConvertReactFlowToFunctions(string nodesJson, string edgesJson)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var nodes = JsonSerializer.Deserialize<List<ReactFlowNode>>(nodesJson, options) ?? new();
        var edges = JsonSerializer.Deserialize<List<ReactFlowEdge>>(edgesJson, options) ?? new();

        // Build execution order based on edges
        var orderedNodes = BuildExecutionOrder(nodes, edges);

        // Convert to WFFunction format (пропуск узлов без functionClass — start/end/condition)
        var functions = orderedNodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Data?.FunctionClass)
                           && !string.IsNullOrWhiteSpace(node.Data?.FunctionName))
            .Select(node => new
            {
                functionclass = node.Data!.FunctionClass!,
                name = node.Data.FunctionName!,
                needParam = GetParamNames(node.Data.NeedParam),
                paramReturn = node.Data.ParamReturn ?? "",
                type = "function",
                sync = true
            }).ToList();

        return JsonSerializer.Serialize(functions);
    }

    // Редактор сохраняет needParam как массив объектов реестра ({name, type, description}),
    // старые шаблоны — как массив строк.
    private static List<string> GetParamNames(List<JsonElement>? needParam)
    {
        if (needParam == null) return new List<string>();

        return needParam
            .Select(p => p.ValueKind switch
            {
                JsonValueKind.String => p.GetString(),
                JsonValueKind.Object when p.TryGetProperty("name", out var name) => name.GetString(),
                _ => null
            })
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToList();
    }

    private static List<ReactFlowNode> BuildExecutionOrder(List<ReactFlowNode> nodes, List<ReactFlowEdge> edges)
    {
        // Build adjacency list
        var graph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        foreach (var node in nodes)
        {
            graph[node.Id] = new List<string>();
            inDegree[node.Id] = 0;
        }

        foreach (var edge in edges)
        {
            graph[edge.Source].Add(edge.Target);
            inDegree[edge.Target]++;
        }

        // Topological sort (Kahn's algorithm)
        var queue = new Queue<string>();
        foreach (var node in nodes)
        {
            if (inDegree[node.Id] == 0)
            {
                queue.Enqueue(node.Id);
            }
        }

        var ordered = new List<ReactFlowNode>();
        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            var node = nodes.First(n => n.Id == nodeId);
            ordered.Add(node);

            foreach (var neighbor in graph[nodeId])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return ordered;
    }
}

public class ReactFlowNode
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public ReactFlowNodeData? Data { get; set; }
}

public class ReactFlowNodeData
{
    public string? FunctionClass { get; set; }
    public string? FunctionName { get; set; }
    public List<JsonElement>? NeedParam { get; set; }
    public string? ParamReturn { get; set; }
}

public class ReactFlowEdge
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}
