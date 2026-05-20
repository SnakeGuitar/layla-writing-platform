using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services
{
    /// <inheritdoc />
    public class GraphApiService : IGraphApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GraphApiService> _logger;

        public GraphApiService(ILogger<GraphApiService> logger)
        {
            _logger = logger;
            _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WorldbuildingApiUrl);
        }

        /// <inheritdoc />
        public async Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null)
        {
            try
            {
                var url = $"/api/graph/{projectId}";
                if (!string.IsNullOrEmpty(entityType))
                    url += $"?type={Uri.EscapeDataString(entityType)}";

                return await _httpClient.GetFromJsonAsync<GraphResult>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GraphApiService] GetGraph failed for project {ProjectId}", projectId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CreateRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId, string type, string? label = null)
        {
            try
            {
                var payload = new { sourceEntityId, targetEntityId, type, label = label ?? type };
                var response = await _httpClient.PostAsJsonAsync($"/api/graph/{projectId}/relationships", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GraphApiService] CreateRelationship failed for project {ProjectId}", projectId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/graph/{projectId}/relationships")
                {
                    Content = JsonContent.Create(new { sourceEntityId, targetEntityId })
                };
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GraphApiService] DeleteRelationship failed for project {ProjectId}", projectId);
                return false;
            }
        }
    }
}
