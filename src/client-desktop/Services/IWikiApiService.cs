using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// Provides CRUD operations for wiki entries on the worldbuilding service,
    /// plus narrative arc queries (entity appearances).
    /// </summary>
    public interface IWikiApiService
    {
        /// <summary>Lists all wiki entries for a project, optionally filtered by entity type.</summary>
        Task<List<WikiEntry>?> GetEntriesAsync(Guid projectId, string? entityType = null);

        /// <summary>Returns a single wiki entry by its entity ID.</summary>
        Task<WikiEntry?> GetEntryAsync(Guid projectId, string entityId);

        /// <summary>Creates a new wiki entry in the project.</summary>
        Task<WikiEntry?> CreateEntryAsync(Guid projectId, string name, string entityType, string? description = null, List<string>? tags = null);

        /// <summary>Updates mutable fields of a wiki entry.</summary>
        Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null);

        /// <summary>Deletes a wiki entry and its Neo4j node.</summary>
        Task<bool> DeleteEntryAsync(Guid projectId, string entityId);

        /// <summary>Returns all chapters where the entity appears (narrative arc).</summary>
        Task<List<AppearanceRecord>?> GetEntityAppearancesAsync(Guid projectId, string entityId);
    }
}
