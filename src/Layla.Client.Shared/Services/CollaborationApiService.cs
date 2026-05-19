using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Layla.Client.Shared.Models;

namespace Layla.Client.Shared.Services;

/// <summary>
/// Concrete implementation of <see cref="ICollaborationApiService"/>.
/// Requires an <see cref="HttpClient"/> pre-configured with the worldbuilding
/// service base URL and an authorization header (injected via DI in both
/// WPF's <c>ServiceLocator</c> and Blazor's DI container).
/// </summary>
public class CollaborationApiService : ICollaborationApiService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public CollaborationApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── Autosave ────────────────────────────────────────────────────────

    public async Task<bool> AutosaveChapterAsync(
        Guid projectId, string manuscriptId, string chapterId,
        string content, IReadOnlyList<MentionPayload>? mentions = null,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new { content, mentions = mentions ?? (IReadOnlyList<MentionPayload>)Array.Empty<MentionPayload>() };
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/autosave",
                payload, JsonOpts, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] Autosave failed: {ex.Message}");
            return false;
        }
    }

    // ── Detectable Entities ─────────────────────────────────────────────

    public async Task<List<DetectableEntity>?> GetDetectableEntitiesAsync(
        Guid projectId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<DetectableEntity>>(
                $"/api/wiki/{projectId}/detectable", ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] GetDetectable failed: {ex.Message}");
            return null;
        }
    }

    // ── Version History ─────────────────────────────────────────────────

    public async Task<List<ChapterVersionMeta>?> GetChapterVersionsAsync(
        Guid projectId, string manuscriptId, string chapterId,
        CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ChapterVersionMeta>>(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions", ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] GetVersions failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ChapterVersionFull?> GetChapterVersionAsync(
        Guid projectId, string manuscriptId, string chapterId, string versionId,
        CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ChapterVersionFull>(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}", ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] GetVersion failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ChapterVersionMeta?> CreateMilestoneAsync(
        Guid projectId, string manuscriptId, string chapterId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/milestone",
                null, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ChapterVersionMeta>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] CreateMilestone failed: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> RestoreVersionAsync(
        Guid projectId, string manuscriptId, string chapterId, string versionId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}/restore",
                null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CollaborationApiService] RestoreVersion failed: {ex.Message}");
            return false;
        }
    }
}
