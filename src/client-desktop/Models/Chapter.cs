using System;
using System.Collections.Generic;

namespace Layla.Desktop.Models
{
    /// <summary>
    /// Represents a chapter embedded within a <see cref="Manuscript"/>.
    /// The <see cref="Content"/> field contains RTF-encoded text and is only
    /// populated when fetching a chapter individually; it is omitted in index responses.
    /// </summary>
    public class Chapter
    {
        /// <summary>UUID assigned by the worldbuilding service upon creation.</summary>
        public Guid ChapterId { get; set; }

        /// <summary>Display title shown in the chapter navigation list.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Full RTF content of the chapter.
        /// Empty string when the chapter was retrieved as part of a manuscript index.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Zero-based position of the chapter within its parent manuscript.</summary>
        public int Order { get; set; }

        /// <summary>Wiki entities detected in this chapter's text by the mention system.</summary>
        public List<Mention> Mentions { get; set; } = new();

        /// <summary>UTC timestamp set by the server when the chapter was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp of the last server-side save.
        /// Used for Last-Write-Wins conflict detection on the worldbuilding service.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
