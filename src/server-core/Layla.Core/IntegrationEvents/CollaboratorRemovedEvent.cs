using System.Text.Json.Serialization;

namespace Layla.Core.IntegrationEvents;

/// <summary>
/// Cross-service contract published to the worldbuilding service via RabbitMQ
/// when a collaborator is removed from a project.
/// </summary>
public class CollaboratorRemovedEvent
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = null!;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;
}
