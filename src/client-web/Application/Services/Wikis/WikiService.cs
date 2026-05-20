using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using client_web.Application.Config.Http;
using client_web.Application.Services.Session;
using client_web.Models.Worldbuilding;
using Microsoft.Extensions.Logging;

namespace client_web.Application.Services.Wikis;

public class WikiService : IWikiService
{
    private readonly WorldbuildingClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<WikiService> _logger;

    public WikiService(WorldbuildingClient client, ISessionManager session, ILogger<WikiService> _logger)
    {
        _client = client;
        _session = session;
        this._logger = _logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<List<WikiEntry>?> GetEntriesAsync(Guid projectId, string? entityType = null)
    {
        try
        {
            var endpoint = $"/api/wiki/{projectId}/entries";
            if (!string.IsNullOrEmpty(entityType))
            {
                endpoint += $"?type={Uri.EscapeDataString(entityType)}";
            }

            return await _client.SendAsync<List<WikiEntry>>(new APIRequest
            {
                Endpoint = endpoint,
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get wiki entries for project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<WikiEntry?> GetEntryAsync(Guid projectId, string entityId)
    {
        try
        {
            return await _client.SendAsync<WikiEntry>(new APIRequest
            {
                Endpoint = $"/api/wiki/{projectId}/entries/{entityId}",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get wiki entry {EntityId}", entityId);
            return null;
        }
    }

    public async Task<WikiEntry?> CreateEntryAsync(Guid projectId, string name, string entityType, string? description = null, List<string>? tags = null)
    {
        try
        {
            return await _client.SendAsync<WikiEntry>(new APIRequest
            {
                Endpoint = $"/api/wiki/{projectId}/entries",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new
                {
                    name,
                    entityType,
                    description = description ?? string.Empty,
                    tags = tags ?? new List<string>()
                }
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to create wiki entry");
            return null;
        }
    }

    public async Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null)
    {
        try
        {
            var payload = new Dictionary<string, object>();
            if (name != null) payload["name"] = name;
            if (entityType != null) payload["entityType"] = entityType;
            if (description != null) payload["description"] = description;
            if (tags != null) payload["tags"] = tags;

            return await _client.SendAsync<WikiEntry>(new APIRequest
            {
                Endpoint = $"/api/wiki/{projectId}/entries/{entityId}",
                Method = HttpMethod.Put,
                Token = Token,
                Body = payload
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to update wiki entry {EntityId}", entityId);
            return null;
        }
    }

    public async Task<bool> DeleteEntryAsync(Guid projectId, string entityId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/wiki/{projectId}/entries/{entityId}",
                Method = HttpMethod.Delete,
                Token = Token
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to delete wiki entry {EntityId}", entityId);
            return false;
        }
    }

    public async Task<List<AppearanceRecord>?> GetEntityAppearancesAsync(Guid projectId, string entityId)
    {
        try
        {
            return await _client.SendAsync<List<AppearanceRecord>>(new APIRequest
            {
                Endpoint = $"/api/wiki/{projectId}/entries/{entityId}/appearances",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get appearances for entity {EntityId} in project {ProjectId}", entityId, projectId);
            return null;
        }
    }
}
