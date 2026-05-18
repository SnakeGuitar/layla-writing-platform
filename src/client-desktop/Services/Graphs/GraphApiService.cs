using Layla.Desktop.Models.Graphs;
using Layla.Desktop.Services.User;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Layla.Desktop.Services.Graphs;

/// <inheritdoc />
public class GraphApiService : IGraphApiService
{
    private readonly HttpClient _httpClient;

    public GraphApiService()
    {
        _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WorldbuildingApiUrl);
    }

    private void AddAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            SessionManager.IsAuthenticated
                ? new AuthenticationHeaderValue("Bearer", SessionManager.CurrentToken)
                : null;
    }

    /// <inheritdoc />
    public async Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null)
    {
        try
        {
            AddAuthorizationHeader();
            var url = $"/api/graph/{projectId}";
            if (!string.IsNullOrEmpty(entityType))
                url += $"?type={Uri.EscapeDataString(entityType)}";

            return await _httpClient.GetFromJsonAsync<GraphResult>(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GraphApiService] GetGraph failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CreateRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId, string type, string? label = null)
    {
        try
        {
            AddAuthorizationHeader();
            var payload = new { sourceEntityId, targetEntityId, type, label = label ?? type };
            var response = await _httpClient.PostAsJsonAsync($"/api/graph/{projectId}/relationships", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GraphApiService] CreateRelationship failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId)
    {
        try
        {
            AddAuthorizationHeader();
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/graph/{projectId}/relationships")
            {
                Content = JsonContent.Create(new { sourceEntityId, targetEntityId })
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GraphApiService] DeleteRelationship failed: {ex.Message}");
            return false;
        }
    }
}
