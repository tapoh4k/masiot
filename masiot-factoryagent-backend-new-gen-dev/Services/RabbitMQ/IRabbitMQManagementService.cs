namespace Backend.Services.RabbitMQ;

public interface IRabbitMQManagementService
{
    Task EnsureUserAsync(string username, string password, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string username, CancellationToken cancellationToken = default);
}
