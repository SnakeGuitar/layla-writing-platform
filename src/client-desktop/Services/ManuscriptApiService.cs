using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;

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

        /// <summary>Initialises the service with a pre-configured <see cref="HttpClient"/>.</summary>
        public ManuscriptApiService()
        {
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
                System.Diagnostics.Debug.WriteLine($"Error retrieving manuscripts: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error retrieving manuscript: {ex.Message}");
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order)
        {
            try
            {
                var payload = new { title, order };
                var response = await _httpClient.PostAsJsonAsync($"/api/manuscripts/{projectId}", payload);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating manuscript: {ex.Message}");
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order)
        {
            try
            {
                var payload = new { title, order };
                var response = await _httpClient.PutAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}", payload);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating manuscript: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error deleting manuscript: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error retrieving chapter: {ex.Message}");
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order)
        {
            try
            {
                var payload = new { title, content, order };
                var response = await _httpClient.PostAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters", payload);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Chapter>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating chapter: {ex.Message}");
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
                var response = await _httpClient.PutAsJsonAsync($"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}", payload);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Chapter>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating chapter: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error deleting chapter: {ex.Message}");
            }
            return false;
        }
    }
}
