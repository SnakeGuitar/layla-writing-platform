using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Layla.Client.Shared.Models;

namespace Layla.Client.Shared.Services;

/// <summary>
/// Interface for the collaboration-specific manuscript HTTP operations
/// (autosave, versioning, milestones, detectable entities).
/// This supplements the existing CRUD APIs already in each client.
/// </summary>
public interface ICollaborationApiService
{
    // ── Autosave ────────────────────────────────────────────────────────

    /// <summary>Sends a debounced autosave with client-detected mentions.</summary>
    Task<bool> AutosaveChapterAsync(
        Guid projectId, string manuscriptId, string chapterId,
        string content, IReadOnlyList<MentionPayload>? mentions = null,
        CancellationToken ct = default);

    // ── Detectable Entities (Tokenizer feed) ────────────────────────────

    /// <summary>
    /// Returns all wiki entities in the format expected by <see cref="Tokenizer.WikiTokenizer"/>.
    /// </summary>
    Task<List<DetectableEntity>?> GetDetectableEntitiesAsync(Guid projectId, CancellationToken ct = default);

    // ── Version History ─────────────────────────────────────────────────

    /// <summary>Returns version history metadata (no content) for a chapter.</summary>
    Task<List<ChapterVersionMeta>?> GetChapterVersionsAsync(
        Guid projectId, string manuscriptId, string chapterId,
        CancellationToken ct = default);

    /// <summary>Returns a specific version including full content.</summary>
    Task<ChapterVersionFull?> GetChapterVersionAsync(
        Guid projectId, string manuscriptId, string chapterId, string versionId,
        CancellationToken ct = default);

    /// <summary>Creates a named milestone (snapshot) of the current chapter state.</summary>
    Task<ChapterVersionMeta?> CreateMilestoneAsync(
        Guid projectId, string manuscriptId, string chapterId,
        CancellationToken ct = default);

    /// <summary>Restores a chapter to a specific version.</summary>
    Task<bool> RestoreVersionAsync(
        Guid projectId, string manuscriptId, string chapterId, string versionId,
        CancellationToken ct = default);
}

/// <summary>
/// Mention payload sent during autosave. Mirrors the server-side schema.
/// </summary>
public class MentionPayload
{
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
}
