using Layla.Desktop.Models.Manuscripts;
using Layla.Desktop.Services.Logger;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Layla.Desktop.Services.Manuscripts;

/// <summary>
/// HTTP client implementation of <see cref="IManuscriptApiService"/>.
/// Targets the Node.js worldbuilding service base URL defined in
/// <see cref="ConfigurationService.WorldbuildingApiUrl"/>.
/// </summary>
public class ManuscriptApiService : IManuscriptApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ManuscriptApiService> _logger;

    /// <summary>Initialises the service with a pre-configured <see cref="HttpClient"/>.</summary>
    public ManuscriptApiService()
    {
        _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WORLDBUILDING_API_URL);
        _logger = Log.For<ManuscriptApiService>();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };


    /// <summary>
    /// Attaches the current session's Bearer token to the outgoing request,
    /// or clears the header when the session is not authenticated.
    /// </summary>
    private void AddAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = SessionManager.IsAuthenticated ? new AuthenticationHeaderValue("Bearer", SessionManager.CurrentToken) : null;
    }


    /// <inheritdoc/>
    public async Task<List<Manuscript>?> GetManuscriptsByProjectAsync(Guid projectId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage? response = await _httpClient.GetAsync(
                $"/api/manuscripts/{projectId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<Manuscript>>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetManuscriptsByProjectAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<Manuscript?> GetManuscriptAsync(Guid projectId, string manuscriptId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage? response = await _httpClient.GetAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Manuscript>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetManuscriptAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order)
    {
        AddAuthorizationHeader();
        try
        {
            var payload = new { title, order };
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                $"/api/manuscripts/{projectId}", payload, JsonOpts);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Manuscript>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("CreateManuscriptAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order)
    {
        AddAuthorizationHeader();
        try
        {
            var payload = new { title, order };
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}", payload, JsonOpts);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Manuscript>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("UpdateManuscriptAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteManuscriptAsync(Guid projectId, string manuscriptId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("DeleteManuscriptAsync - Method exception: {exception}" + ex.ToString());
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<Chapter?> GetChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Chapter>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetChapterAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order)
    {
        AddAuthorizationHeader();
        try
        {
            var payload = new { title, content, order };
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters", payload, JsonOpts);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Chapter>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("CreateChapterAsync() - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<Chapter?> UpdateChapterAsync(Guid projectId, string manuscriptId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null)
    {
        AddAuthorizationHeader();
        try
        {
            var payload = new
            {
                title,
                content,
                order,
                clientTimestamp = clientTimestamp?.ToString("o")
            };
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}", payload, JsonOpts);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Chapter>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("UpdateChapterAsync - Method exception: {exception}" + ex.ToString());
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("DeleteChapterAsync() - Method exception: {exception}" + ex.ToString());
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> CreateMilestoneAsync(Guid projectId, string manuscriptId, Guid chapterId, string content)
    {
        AddAuthorizationHeader();
        try
        {
            var payload = new
            {
                content,
                mentions = Array.Empty<object>(),
                isMilestone = true
            };
            string json = JsonSerializer.Serialize(payload);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/autosave", httpContent);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating milestone: {ex.Message}");
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<List<ChapterVersion>?> GetChapterVersionsAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        AddAuthorizationHeader();
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ChapterVersion>>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting chapter versions: {ex.Message}");
        }
        return null;
    }
}
