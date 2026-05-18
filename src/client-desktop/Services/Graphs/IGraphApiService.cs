using Layla.Desktop.Models.Graphs;

namespace Layla.Desktop.Services.Graphs;

/// <summary>
/// Provides operations for the narrative graph on the worldbuilding service.
/// </summary>
public interface IGraphApiService
{
    /// <summary>Returns the full narrative graph (nodes + edges) for a project.</summary>
    Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null);

    /// <summary>Creates a directed relationship between two entities.</summary>
    Task<bool> CreateRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId, string type, string? label = null);

    /// <summary>Deletes all relationships between two entities.</summary>
    Task<bool> DeleteRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId);
}
