using System;

namespace Layla.Client.Shared.Models;

/// <summary>
/// Metadata for a chapter version (content excluded for list performance).
/// Mirrors the MongoDB document with <c>-content</c> projection.
/// </summary>
public class ChapterVersionMeta
{
    /// <summary>MongoDB ObjectId.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public string ChapterId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public bool IsMilestone { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Full chapter version including content, used for diff previews and restores.
/// </summary>
public class ChapterVersionFull : ChapterVersionMeta
{
    public string Content { get; set; } = string.Empty;
}
