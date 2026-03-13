using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
    public class ManuscriptApiService : IManuscriptApiService
    {
        private readonly HttpClient _httpClient;
        public ManuscriptApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ConfigurationService.WorldbuildingApiUrl)
            };
        }

        private void AddAuthorizationHeader()
        {
            if (SessionManager.IsAuthenticated)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.CurrentToken);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<Manuscript> GetManuscriptAsync(Guid projectId)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"/api/manuscripts/{projectId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Manuscript>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving manuscript: {ex.Message}");
            }
            return null;
        }

        public async Task<Chapter> GetChapterAsync(Guid projectId, Guid chapterId)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"/api/manuscripts/{projectId}/chapters/{chapterId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Chapter>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving chapter: {ex.Message}");
            }
            return null;
        }

        public async Task<Chapter> CreateChapterAsync(Guid projectId, string title, string content, int order)
        {
            AddAuthorizationHeader();
            try
            {
                var payload = new { title, content, order };
                var response = await _httpClient.PostAsJsonAsync($"/api/manuscripts/{projectId}/chapters", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Chapter>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating chapter: {ex.Message}");
            }
            return null;
        }

        public async Task<Chapter> UpdateChapterAsync(Guid projectId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null)
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

                var response = await _httpClient.PutAsJsonAsync($"/api/manuscripts/{projectId}/chapters/{chapterId}", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Chapter>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating chapter: {ex.Message}");
            }
            return null;
        }
    }
}
