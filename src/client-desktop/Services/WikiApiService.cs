using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
    /// <inheritdoc />
    public class WikiApiService : IWikiApiService
    {
        private readonly HttpClient _httpClient;

        public WikiApiService()
        {
            _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WorldbuildingApiUrl);
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
                var url = $"/api/wiki/{projectId}/entries";
                if (!string.IsNullOrEmpty(entityType))
                    url += $"?type={Uri.EscapeDataString(entityType)}";

                return await _httpClient.GetFromJsonAsync<List<WikiEntry>>(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WikiApiService] GetEntries failed: {ex.Message}");
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
                Debug.WriteLine($"[WikiApiService] GetEntry failed: {ex.Message}");
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
                var response = await _httpClient.PostAsJsonAsync($"/api/wiki/{projectId}/entries", payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WikiEntry>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WikiApiService] CreateEntry failed: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null)
        {
            try
            {
                AddAuthorizationHeader();
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
                Debug.WriteLine($"[WikiApiService] UpdateEntry failed: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteEntryAsync(Guid projectId, string entityId)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"/api/wiki/{projectId}/entries/{entityId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WikiApiService] DeleteEntry failed: {ex.Message}");
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
                Debug.WriteLine($"[WikiApiService] GetEntityAppearances failed: {ex.Message}");
                return null;
            }
        }
    }
}
