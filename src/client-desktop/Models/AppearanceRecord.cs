namespace Layla.Desktop.Models
{
    /// <summary>
    /// Describes a chapter in which a wiki entity appears,
    /// as tracked by APPEARS_IN edges in the narrative graph.
    /// </summary>
    public class AppearanceRecord
    {
        /// <summary>UUID of the manuscript containing the chapter.</summary>
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>Display title of the manuscript.</summary>
        public string ManuscriptTitle { get; set; } = string.Empty;

        /// <summary>UUID of the chapter.</summary>
        public string ChapterId { get; set; } = string.Empty;

        /// <summary>Display title of the chapter.</summary>
        public string ChapterTitle { get; set; } = string.Empty;
    }
}
