using System.Globalization;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Backend.Services.Workflow;

/// <summary>
/// Нормализует параметры workflow: snake_case → camelCase, разворачивает JsonElement/JToken.
/// </summary>
public static class WorkflowParameterNormalizer
{
    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tech_process_id"] = "techProcessId",
        ["tech_process_code"] = "techProcessCode",
        ["tech_process_name"] = "techProcessName",
        ["wf_id"] = "wfId",
        ["op_no"] = "opNo",
        ["order_number"] = "orderNumber",
        ["operation_number"] = "operationNumber",
        ["current_op_no"] = "currentOpNo",
        ["delay_seconds"] = "delaySeconds",
        ["type_plan"] = "typePlan",
        ["work_message_type"] = "workMessageType",
        ["message_type"] = "messageType",
        ["routing_key"] = "routingKey",
        ["time_end"] = "timeEnd",
        ["delta_time"] = "deltaTime",
        ["shift_time"] = "shiftTime",
    };

    public static Dictionary<string, object> Normalize(
        IDictionary<string, object>? raw,
        int? techProcessId = null,
        int? wfId = null)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (raw != null)
        {
            foreach (var (key, value) in raw)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var normalizedKey = KeyAliases.TryGetValue(key, out var alias) ? alias : key;
                var unwrapped = Unwrap(value);
                if (unwrapped != null)
                    result[normalizedKey] = unwrapped;
            }
        }

        if (techProcessId.HasValue)
            result["techProcessId"] = techProcessId.Value;

        if (wfId.HasValue)
            result["wfId"] = wfId.Value;

        return result;
    }

    public static Dictionary<string, object> FromJson(string? json, int? techProcessId = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Normalize(null, techProcessId);

        // Newtonsoft даёт long/double/JToken — удобнее для workflow engine
        var raw = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                  ?? new Dictionary<string, object>();
        return Normalize(raw, techProcessId);
    }

    public static object? Unwrap(object? value)
    {
        if (value == null) return null;

        if (value is JsonElement je)
            return UnwrapJsonElement(je);

        if (value is JValue jv)
            return jv.Value;

        if (value is JObject jObj)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in jObj.Properties())
                dict[prop.Name] = Unwrap(prop.Value);
            return dict;
        }

        if (value is JArray jArr)
            return jArr.Select(Unwrap).ToList();

        return value;
    }

    private static object? UnwrapJsonElement(JsonElement je)
    {
        return je.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => je.GetString(),
            JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
            JsonValueKind.Array => je.EnumerateArray().Select(UnwrapJsonElement).ToList(),
            JsonValueKind.Object => je.EnumerateObject()
                .ToDictionary(p => p.Name, p => UnwrapJsonElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => je.ToString()
        };
    }

    public static int? AsInt(object? value)
    {
        var unwrapped = Unwrap(value);
        if (unwrapped == null) return null;
        if (unwrapped is int i) return i;
        if (unwrapped is long l) return checked((int)l);
        if (unwrapped is double d) return (int)d;
        if (int.TryParse(Convert.ToString(unwrapped, CultureInfo.InvariantCulture), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }
}
