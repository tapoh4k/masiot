using Newtonsoft.Json;

namespace Backend.Models.Messages;

public class Message
{
    [JsonProperty("messageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonProperty("sender_id")]
    public string SenderId { get; set; } = string.Empty;

    [JsonProperty("receiver_id")]
    public string ReceiverId { get; set; } = string.Empty;

    public Message() { }

    public Message(string messageType, string senderId, string receiverId)
    {
        MessageType = messageType;
        SenderId = senderId;
        ReceiverId = receiverId;
    }
}
