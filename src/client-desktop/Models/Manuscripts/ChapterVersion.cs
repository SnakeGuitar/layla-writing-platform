namespace Layla.Desktop.Models.Manuscripts;

/// <summary>
/// Represents a saved snapshot (version) of a chapter's content.
/// Returned by the worldbuilding service's version-history endpoints.
/// <see cref="IsMilestone"/> distinguishes user-flagged milestone snapshots
/// from regular autosaves.
/// </summary>
public class ChapterVersion
{
    /// <summary>UUID of this version snapshot.</summary>
    public string VersionId { get; set; } = string.Empty;

    /// <summary>The chapter this version belongs to.</summary>
    public string ChapterId { get; set; } = string.Empty;

    /// <summary>The RTF content captured at save time. Omitted in list responses.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Username / user-ID that triggered the save.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this version was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// <c>true</c> when the user explicitly flagged this snapshot as a milestone.
    /// Displayed with an orange MILESTONE badge in the history panel.
    /// </summary>
    public bool IsMilestone { get; set; }
}
