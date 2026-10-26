namespace Backend.Services.RabbitMQ;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(string messageJson, string messageType);
}
