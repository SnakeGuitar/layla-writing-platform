using System;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
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
}
