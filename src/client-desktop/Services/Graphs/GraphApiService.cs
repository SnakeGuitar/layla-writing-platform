using Layla.Desktop.Models.Graphs;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;

namespace Layla.Desktop.Services.Graphs;

/// <inheritdoc />
public class GraphApiService : IGraphApiService
{
    private readonly HttpClient _httpClient;

    public GraphApiService()
    {
        this._httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.WORLDBUILDING_API_URL);
    }

    private void AddAuthorizationHeader()
    {
        this._httpClient.DefaultRequestHeaders.Authorization =
            SessionManager.IsAuthenticated
                ? new("Bearer", SessionManager.CurrentToken)
                : null;
    }

    /// <inheritdoc />
    public async Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null)
    {
        try
        {
            AddAuthorizationHeader();
            string? url = $"/api/graph/{projectId}";
            if (!string.IsNullOrEmpty(entityType))
                url += $"?type={Uri.EscapeDataString(entityType)}";

            return await this._httpClient.GetFromJsonAsync<GraphResult>(url);
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
            var payload =
                new { sourceEntityId, targetEntityId, type, label = label ?? type };
            HttpResponseMessage? response = await this._httpClient.PostAsJsonAsync($"/api/graph/{projectId}/relationships", payload);
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
            HttpRequestMessage request = new(HttpMethod.Delete, $"/api/graph/{projectId}/relationships")
            {
                Content = JsonContent.Create(new { sourceEntityId, targetEntityId })
            };
            HttpResponseMessage response = await this._httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GraphApiService] DeleteRelationship failed: {ex.Message}");
            return false;
        }
    }
}
