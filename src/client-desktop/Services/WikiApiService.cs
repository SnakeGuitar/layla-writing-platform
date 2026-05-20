using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services
{
    /// <inheritdoc />
    public class WikiApiService : IWikiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WikiApiService> _logger;

        public WikiApiService(ILogger<WikiApiService> logger)
        {
            _logger = logger;
            _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WorldbuildingApiUrl);
        }


        /// <inheritdoc />
        public async Task<List<WikiEntry>?> GetEntriesAsync(Guid projectId, string? entityType = null)
        {
            try
            {
                var url = $"/api/wiki/{projectId}/entries";
                if (!string.IsNullOrEmpty(entityType))
                    url += $"?type={Uri.EscapeDataString(entityType)}";

                return await _httpClient.GetFromJsonAsync<List<WikiEntry>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEntries failed for project {ProjectId}", projectId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WikiEntry?> GetEntryAsync(Guid projectId, string entityId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<WikiEntry>($"/api/wiki/{projectId}/entries/{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEntry failed for entry {EntityId} in project {ProjectId}", entityId, projectId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WikiEntry?> CreateEntryAsync(Guid projectId, string name, string entityType, string? description = null, List<string>? tags = null)
        {
            try
            {
                var payload = new { name, entityType, description = description ?? "", tags = tags ?? new List<string>() };
                var response = await _httpClient.PostAsJsonAsync($"/api/wiki/{projectId}/entries", payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WikiEntry>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateEntry failed for project {ProjectId}", projectId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null)
        {
            try
            {
                var payload = new Dictionary<string, object>();
                if (name != null) payload["name"] = name;
                if (entityType != null) payload["entityType"] = entityType;
                if (description != null) payload["description"] = description;
                if (tags != null) payload["tags"] = tags;

                var response = await _httpClient.PutAsJsonAsync($"/api/wiki/{projectId}/entries/{entityId}", payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WikiEntry>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateEntry failed for entry {EntityId} in project {ProjectId}", entityId, projectId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteEntryAsync(Guid projectId, string entityId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/wiki/{projectId}/entries/{entityId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteEntry failed for entry {EntityId} in project {ProjectId}", entityId, projectId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<AppearanceRecord>?> GetEntityAppearancesAsync(Guid projectId, string entityId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<AppearanceRecord>>(
                    $"/api/wiki/{projectId}/entries/{entityId}/appearances");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEntityAppearances failed for entry {EntityId} in project {ProjectId}", entityId, projectId);
                return null;
            }
        }
    }
}
