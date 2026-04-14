using System;
using System.Collections.Generic;

namespace Layla.Desktop.Models
{
    /// <summary>
    /// Represents a single manuscript belonging to a project.
    /// A project may own multiple manuscripts, each identified by <see cref="ManuscriptId"/>.
    /// </summary>
    public class Manuscript
    {
        /// <summary>UUID assigned by the worldbuilding service upon creation.</summary>
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>UUID of the owning project, issued by server-core.</summary>
        public Guid ProjectId { get; set; }

        /// <summary>Human-readable title shown in the editor sidebar.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Zero-based display order among the project's manuscripts.</summary>
        public int Order { get; set; }

        /// <summary>
        /// Chapter index for this manuscript.
        /// In index responses the <see cref="Chapter.Content"/> field is omitted.
        /// </summary>
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();

        /// <summary>UTC timestamp set by the server when the document was first created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last server-side update, or <c>null</c> if never updated.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
