using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Layla.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// HTTP client implementation of <see cref="IManuscriptApiService"/>.
    /// Targets the Node.js worldbuilding service base URL defined in
    /// <see cref="ConfigurationService.WorldbuildingApiUrl"/>.
    /// </summary>
    public class ManuscriptApiService : IManuscriptApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ManuscriptApiService> _logger;

        // Skip null properties when serialising request bodies. Required because
        // the worldbuilding Zod schemas use `.optional()` (matches `undefined`)
        // and reject explicit `null` with a 400 — so sending
        // `{ clientTimestamp: null }` was silently breaking chapter saves and
        // forcing every update into the offline-cache fallback.
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Initialises the service with a pre-configured <see cref="HttpClient"/>.</summary>
        public ManuscriptApiService(ILogger<ManuscriptApiService> logger)
        {
            _logger = logger;
            _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WorldbuildingApiUrl);
        }

        /// <inheritdoc/>
        public async Task<List<Manuscript>?> GetManuscriptsByProjectAsync(Guid projectId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/manuscripts/{projectId}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<List<Manuscript>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manuscripts for project {ProjectId}", projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Manuscript?> GetManuscriptAsync(Guid projectId, string manuscriptId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/manuscripts/{projectId}/{manuscriptId}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manuscript {ManuscriptId} for project {ProjectId}", manuscriptId, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order)
        {
            try
            {
                var payload = new { title, order };
                var response = await _httpClient.PostAsJsonAsync($"/api/manuscripts/{projectId}", payload, JsonOpts);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manuscript '{Title}' for project {ProjectId}", title, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order)
        {
            try
            {
                var payload = new { title, order };
                var response = await _httpClient.PutAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}", payload, JsonOpts);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manuscript {ManuscriptId} for project {ProjectId}", manuscriptId, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteManuscriptAsync(Guid projectId, string manuscriptId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/manuscripts/{projectId}/{manuscriptId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting manuscript {ManuscriptId} for project {ProjectId}", manuscriptId, projectId);
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<Chapter?> GetChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Chapter>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chapter {ChapterId} for project {ProjectId}", chapterId, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order)
        {
            try
            {
                var payload = new { title, content, order };
                var response = await _httpClient.PostAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters", payload, JsonOpts);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Chapter>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter '{Title}' in manuscript {ManuscriptId} for project {ProjectId}", title, manuscriptId, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Chapter?> UpdateChapterAsync(Guid projectId, string manuscriptId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null)
        {
            try
            {
                var payload = new
                {
                    title,
                    content,
                    order,
                    clientTimestamp = clientTimestamp?.ToString("o")
                };
                var response = await _httpClient.PutAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}", payload, JsonOpts);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Chapter>();

                // Surface the failure reason — otherwise every server rejection
                // (400 from a malformed payload, 401 from an expired token,
                // 404 from a stale chapterId) looked identical from the VM.
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Chapter UPDATE {StatusCode}: {Body}", response.StatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId} in manuscript {ManuscriptId} for project {ProjectId}", chapterId, manuscriptId, projectId);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chapter {ChapterId} in manuscript {ManuscriptId} for project {ProjectId}", chapterId, manuscriptId, projectId);
            }
            return false;
        }
    }
}
