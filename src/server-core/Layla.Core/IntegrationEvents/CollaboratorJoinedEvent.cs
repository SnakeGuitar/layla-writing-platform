using System.Text.Json.Serialization;

namespace Layla.Core.IntegrationEvents;

/// <summary>
/// Cross-service contract published to the worldbuilding service via RabbitMQ
/// when a collaborator joins or is added to a project.
/// </summary>
public class CollaboratorJoinedEvent
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = null!;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("joinedAt")]
    public DateTime JoinedAt { get; set; }
}
