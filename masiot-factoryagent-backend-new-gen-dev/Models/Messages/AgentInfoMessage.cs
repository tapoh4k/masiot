using Newtonsoft.Json;

namespace Backend.Models.Messages;

public class AgentInfoMessage : Message
{
    [JsonProperty("parameter")]
    public Dictionary<string, object> Parameter { get; set; } = new();

    public AgentInfoMessage() { }

    public AgentInfoMessage(string messageType, string senderId, string receiverId, Dictionary<string, object> parameter)
        : base(messageType, senderId, receiverId)
    {
        Parameter = parameter;
    }
}
