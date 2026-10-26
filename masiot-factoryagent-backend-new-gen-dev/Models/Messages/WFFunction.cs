using Newtonsoft.Json;

namespace Backend.Models.Messages;

[Serializable]
public class WFFunction
{
    [JsonProperty("functionclass")]
    public string FunctionClass { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("needParam")]
    public List<string> NeedParam { get; set; } = new();

    [JsonProperty("paramReturn")]
    public string ParamReturn { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("sync")]
    public bool Sync { get; set; }

    public WFFunction() { }

    public WFFunction(string functionClass, string name, string type, string paramReturn, List<string> needParam, bool sync)
    {
        FunctionClass = functionClass;
        Name = name;
        Type = type;
        ParamReturn = paramReturn;
        NeedParam = needParam;
        Sync = sync;
    }
}
