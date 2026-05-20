using System;
using System.Net.Http;
using System.Threading.Tasks;
using client_web.Application.Config.Http;
using client_web.Application.Services.Session;
using client_web.Models.Worldbuilding;
using Microsoft.Extensions.Logging;

namespace client_web.Application.Services.Graph;

public class GraphService : IGraphService
{
    private readonly WorldbuildingClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<GraphService> _logger;

    public GraphService(WorldbuildingClient client, ISessionManager session, ILogger<GraphService> logger)
    {
        _client = client;
        _session = session;
        _logger = logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null)
    {
        try
        {
            var endpoint = $"/api/graph/{projectId}";
            if (!string.IsNullOrEmpty(entityType))
            {
                endpoint += $"?type={Uri.EscapeDataString(entityType)}";
            }

            return await _client.SendAsync<GraphResult>(new APIRequest
            {
                Endpoint = endpoint,
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get narrative graph for project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<bool> CreateRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId, string type, string? label = null)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/graph/{projectId}/relationships",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new
                {
                    sourceEntityId,
                    targetEntityId,
                    type,
                    label = label ?? type
                }
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to create relationship between {SourceId} and {TargetId}", sourceEntityId, targetEntityId);
            return false;
        }
    }

    public async Task<bool> DeleteRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/graph/{projectId}/relationships",
                Method = HttpMethod.Delete,
                Token = Token,
                Body = new
                {
                    sourceEntityId,
                    targetEntityId
                }
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to delete relationship between {SourceId} and {TargetId}", sourceEntityId, targetEntityId);
            return false;
        }
    }
}
