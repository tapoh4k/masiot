using Newtonsoft.Json;

namespace Backend.Models.Messages;

public class NewWFMessage : Message
{
    [JsonProperty("wFType")]
    public string WFType { get; set; } = string.Empty;

    [JsonProperty("wFParameter")]
    public Dictionary<string, object> WFParameter { get; set; } = new();

    [JsonProperty("wFFunctions")]
    public List<WFFunction> WFFunctions { get; set; } = new();

    public NewWFMessage() { }

    public NewWFMessage(string messageType, string senderId, string receiverId,
        string wfType, Dictionary<string, object> wfParameter, List<WFFunction> wfFunctions)
        : base(messageType, senderId, receiverId)
    {
        WFType = wfType;
        WFParameter = wfParameter;
        WFFunctions = wfFunctions;
    }
}
