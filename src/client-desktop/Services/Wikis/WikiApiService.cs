using Layla.Desktop.Models.Wikis;
using Layla.Desktop.Services.Logger;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace Layla.Desktop.Services.Wikis;

/// <inheritdoc />
public class WikiApiService : IWikiApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikiApiService> _logger;

    public WikiApiService()
    {
        _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WORLDBUILDING_API_URL);
        _logger = Log.For<WikiApiService>();
    }

    private void AddAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            SessionManager.IsAuthenticated
                ? new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.CurrentToken)
                : null;
    }

    /// <inheritdoc />
    public async Task<List<WikiEntry>?> GetEntriesAsync(Guid projectId, string? entityType = null)
    {
        try
        {
            AddAuthorizationHeader();
            string url = $"/api/wiki/{projectId}/entries";
            if (!string.IsNullOrEmpty(entityType))
                url += $"?type={Uri.EscapeDataString(entityType)}";

            return await _httpClient.GetFromJsonAsync<List<WikiEntry>>(url);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetEntriesAsync() - Method exception: {exception}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<WikiEntry?> GetEntryAsync(Guid projectId, string entityId)
    {
        try
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<WikiEntry>($"/api/wiki/{projectId}/entries/{entityId}");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetEntryAsync() - Method exception: {exception}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<WikiEntry?> CreateEntryAsync(Guid projectId, string name, string entityType, string? description = null, List<string>? tags = null)
    {
        try
        {
            AddAuthorizationHeader();
            var payload = new { name, entityType, description = description ?? "", tags = tags ?? new List<string>() };
            HttpResponseMessage? response = await _httpClient.PostAsJsonAsync($"/api/wiki/{projectId}/entries", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WikiEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("CreateEntryAsync() - Method exception: {exception}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null)
    {
        try
        {
            AddAuthorizationHeader();
            Dictionary<string, object>? payload = new();
            if (name != null) payload["name"] = name;
            if (entityType != null) payload["entityType"] = entityType;
            if (description != null) payload["description"] = description;
            if (tags != null) payload["tags"] = tags;

            HttpResponseMessage? response = await _httpClient.PutAsJsonAsync($"/api/wiki/{projectId}/entries/{entityId}", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WikiEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("UpdateEntryAsync() - Method exception: {exception}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteEntryAsync(Guid projectId, string entityId)
    {
        try
        {
            AddAuthorizationHeader();
            HttpResponseMessage? response = await _httpClient.DeleteAsync($"/api/wiki/{projectId}/entries/{entityId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("DeleteEntryAsync() - Method exception: {exception}" + ex.ToString());
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<AppearanceRecord>?> GetEntityAppearancesAsync(Guid projectId, string entityId)
    {
        try
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<AppearanceRecord>>(
                $"/api/wiki/{projectId}/entries/{entityId}/appearances");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("GetEntityAppearancesAsync() - Method exception: {exception}", ex.ToString());
            return null;
        }
    }
}
