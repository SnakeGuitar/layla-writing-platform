using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using client_web.Application.Config.Http;
using client_web.Application.Services.Session;
using client_web.Models.Worldbuilding;

namespace client_web.Application.Services.Manuscripts;

public class ManuscriptService : IManuscriptService
{
    private readonly WorldbuildingClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<ManuscriptService> _logger;

    public ManuscriptService(WorldbuildingClient client, ISessionManager session, ILogger<ManuscriptService> logger)
    {
        _client = client;
        _session = session;
        _logger = logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<List<Manuscript>?> GetManuscriptsByProjectAsync(Guid projectId)
    {
        try
        {
            return await _client.SendAsync<List<Manuscript>>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get manuscripts for project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<Manuscript?> GetManuscriptAsync(Guid projectId, string manuscriptId)
    {
        try
        {
            return await _client.SendAsync<Manuscript>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get manuscript {ManuscriptId}", manuscriptId);
            return null;
        }
    }

    public async Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order)
    {
        try
        {
            return await _client.SendAsync<Manuscript>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new { title, order }
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to create manuscript");
            return null;
        }
    }

    public async Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order)
    {
        try
        {
            return await _client.SendAsync<Manuscript>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}",
                Method = HttpMethod.Put,
                Token = Token,
                Body = new { title, order }
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to update manuscript {ManuscriptId}", manuscriptId);
            return null;
        }
    }

    public async Task<bool> DeleteManuscriptAsync(Guid projectId, string manuscriptId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}",
                Method = HttpMethod.Delete,
                Token = Token
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to delete manuscript {ManuscriptId}", manuscriptId);
            return false;
        }
    }

    public async Task<Chapter?> GetChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        try
        {
            return await _client.SendAsync<Chapter>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get chapter {ChapterId}", chapterId);
            return null;
        }
    }

    public async Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order)
    {
        try
        {
            return await _client.SendAsync<Chapter>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new { title, content, order }
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to create chapter");
            return null;
        }
    }

    public async Task<Chapter?> UpdateChapterAsync(Guid projectId, string manuscriptId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null)
    {
        try
        {
            return await _client.SendAsync<Chapter>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}",
                Method = HttpMethod.Put,
                Token = Token,
                Body = new 
                { 
                    title, 
                    content, 
                    order, 
                    clientTimestamp = clientTimestamp?.ToString("o") 
                }
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to update chapter {ChapterId}. Error: {Message}", chapterId, ex.Message);
            return null;
        }
    }

    public async Task<bool> DeleteChapterAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}",
                Method = HttpMethod.Delete,
                Token = Token
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to delete chapter {ChapterId}", chapterId);
            return false;
        }
    }

    public async Task<List<Layla.Client.Shared.Models.ChapterVersionMeta>?> GetChapterVersionsAsync(Guid projectId, string manuscriptId, Guid chapterId)
    {
        try
        {
            return await _client.SendAsync<List<Layla.Client.Shared.Models.ChapterVersionMeta>>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get chapter versions for chapter {ChapterId}", chapterId);
            return null;
        }
    }

    public async Task<Layla.Client.Shared.Models.ChapterVersionFull?> GetChapterVersionAsync(Guid projectId, string manuscriptId, Guid chapterId, string versionId)
    {
        try
        {
            return await _client.SendAsync<Layla.Client.Shared.Models.ChapterVersionFull>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to get chapter version {VersionId} for chapter {ChapterId}", versionId, chapterId);
            return null;
        }
    }

    public async Task<bool> RestoreChapterVersionAsync(Guid projectId, string manuscriptId, Guid chapterId, string versionId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}/restore",
                Method = HttpMethod.Put,
                Token = Token
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Failed to restore chapter version {VersionId} for chapter {ChapterId}", versionId, chapterId);
            return false;
        }
    }
}
