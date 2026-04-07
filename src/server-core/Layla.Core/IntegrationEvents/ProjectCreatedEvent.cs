using System.Text.Json.Serialization;

namespace Layla.Core.IntegrationEvents;

/// <summary>
/// Cross-service contract published to the worldbuilding service via RabbitMQ
/// when a new project is created. The <see cref="SchemaVersion"/> field lets
/// consumers detect breaking schema evolution and reject older/newer payloads.
/// </summary>
public class ProjectCreatedEvent
{
    /// <summary>Current schema version for this contract. Bump on any breaking change.</summary>
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = null!;

    [JsonPropertyName("ownerId")]
    public string OwnerId { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
