using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class HostDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "cluster";
    public string Status { get; set; } = "offline";

    [JsonPropertyName("max_agents")]
    public int MaxAgents { get; set; }

    [JsonPropertyName("deploy_target")]
    public string DeployTarget { get; set; } = "docker";

    [JsonPropertyName("rmq_username")]
    public string? RmqUsername { get; set; }

    [JsonPropertyName("queue_name")]
    public string QueueName { get; set; } = string.Empty;

    [JsonPropertyName("routing_key")]
    public string RoutingKey { get; set; } = string.Empty;

    [JsonPropertyName("agents_count")]
    public int AgentsCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateHostDto
{
    [Required(ErrorMessage = "Имя хоста обязательно")]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(cluster|single|specific)$", ErrorMessage = "Тип: cluster, single или specific")]
    public string Type { get; set; } = "cluster";

    [JsonPropertyName("max_agents")]
    [Range(1, 100000)]
    public int? MaxAgents { get; set; }

    [JsonPropertyName("deploy_target")]
    [RegularExpression("^(docker|machine|custom)$", ErrorMessage = "deploy_target: docker, machine или custom")]
    public string? DeployTarget { get; set; }
}

public class UpdateHostDto
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    [RegularExpression("^(online|offline|unknown)$")]
    public string? Status { get; set; }

    [JsonPropertyName("max_agents")]
    [Range(1, 100000)]
    public int? MaxAgents { get; set; }

    [JsonPropertyName("deploy_target")]
    [RegularExpression("^(docker|machine|custom)$")]
    public string? DeployTarget { get; set; }
}
