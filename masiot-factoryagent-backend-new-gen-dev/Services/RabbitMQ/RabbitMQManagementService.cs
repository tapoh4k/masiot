using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services.RabbitMQ;

/// <summary>
/// RabbitMQ Management HTTP API (порт 15672) для создания пользователей Host.
/// </summary>
public class RabbitMQManagementService : IRabbitMQManagementService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RabbitMQManagementService> _logger;

    public RabbitMQManagementService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<RabbitMQManagementService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var host = _configuration["RabbitMQ:Host"] ?? "localhost";
        var mgmtPort = _configuration["RabbitMQ:ManagementPort"] ?? "15672";
        var user = _configuration["RabbitMQ:Username"] ?? "guest";
        var pass = _configuration["RabbitMQ:Password"] ?? "guest";

        var client = _httpClientFactory.CreateClient(nameof(RabbitMQManagementService));
        client.BaseAddress = new Uri($"http://{host}:{mgmtPort}/api/");
        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
        return client;
    }

    public async Task EnsureUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();

        var putUser = new StringContent(
            JsonSerializer.Serialize(new { password, tags = "" }),
            Encoding.UTF8,
            "application/json");

        var userResp = await client.PutAsync($"users/{Uri.EscapeDataString(username)}", putUser, cancellationToken);
        if (!userResp.IsSuccessStatusCode)
        {
            var body = await userResp.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("RabbitMQ create user {User} failed: {Status} {Body}", username, userResp.StatusCode, body);
            throw new InvalidOperationException($"Не удалось создать RabbitMQ user '{username}': {userResp.StatusCode}");
        }

        // Permissions on default vhost /
        var perms = new StringContent(
            JsonSerializer.Serialize(new { configure = ".*", write = ".*", read = ".*" }),
            Encoding.UTF8,
            "application/json");

        var permResp = await client.PutAsync(
            $"permissions/%2F/{Uri.EscapeDataString(username)}",
            perms,
            cancellationToken);

        if (!permResp.IsSuccessStatusCode)
        {
            var body = await permResp.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("RabbitMQ set permissions for {User} failed: {Status} {Body}", username, permResp.StatusCode, body);
            throw new InvalidOperationException($"Не удалось выдать permissions пользователю '{username}'");
        }

        _logger.LogInformation("RabbitMQ user ensured: {User}", username);
    }

    public async Task DeleteUserAsync(string username, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var resp = await client.DeleteAsync($"users/{Uri.EscapeDataString(username)}", cancellationToken);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("RabbitMQ delete user {User} failed: {Status} {Body}", username, resp.StatusCode, body);
        }
    }
}
