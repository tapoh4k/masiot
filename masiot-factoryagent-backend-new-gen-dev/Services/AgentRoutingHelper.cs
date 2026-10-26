using System.Text.Json;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Канонические AgentId / routing keys по ТЗ Host.
/// Очередь и personal key = AgentId (agent.{id}); profession/okpd2 — без точек.
/// </summary>
public static class AgentRoutingHelper
{
    public static string BuildAgentId(int agentId) => $"agent.{agentId}";

    public static string BuildHostRoutingKey(int hostId) => $"Host.{hostId}";

    public static string BuildHostQueueName(int hostId) => $"Host.{hostId}";

    /// <summary>Точки в profession/okpd2-кодах для routing key заменяем на _.</summary>
    public static string NormalizeRoutingCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return code.Trim().Replace('.', '_');
    }

    public static List<string> GetProfessionCodes(Agent agent)
    {
        var codes = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.Profession))
            codes.Add(agent.Profession);

        if (!string.IsNullOrWhiteSpace(agent.Okpd2Code))
        {
            foreach (var part in agent.Okpd2Code.Split(new[] { ',', ';', '|', '\n', '\r' },
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                codes.Add(part);
            }
        }

        if (!string.IsNullOrWhiteSpace(agent.Okpd2Codes) && agent.Okpd2Codes != "[]")
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(agent.Okpd2Codes);
                if (list != null)
                    codes.AddRange(list.Where(c => !string.IsNullOrWhiteSpace(c)));
            }
            catch
            {
                // ignore malformed jsonb
            }
        }

        return codes
            .Select(NormalizeRoutingCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Routing keys: AgentId + нормализованные коды профессий/ОКПД.</summary>
    public static List<string> GetAgentRoutingKeys(Agent agent)
    {
        var agentId = BuildAgentId(agent.Id);
        var keys = new List<string> { agentId };
        keys.AddRange(GetProfessionCodes(agent));
        return keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
