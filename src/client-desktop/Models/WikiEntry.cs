using System;
using System.Collections.Generic;

namespace Layla.Desktop.Models
{
    /// <summary>
    /// Represents a wiki entry in the worldbuilding service.
    /// Wiki entries are the building blocks of the narrative graph:
    /// characters, locations, events, and objects.
    /// </summary>
    public class WikiEntry
    {
        /// <summary>UUID assigned by the worldbuilding service.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>UUID of the owning project.</summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Display name of the entity (e.g. "Elena Voss").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Category of the entity.</summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Free-text description of the entity.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>User-defined classification tags.</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Whether the entry has been synced to Neo4j.</summary>
        public bool Neo4jSynced { get; set; }

        /// <summary>UTC creation timestamp.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC last-update timestamp.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
