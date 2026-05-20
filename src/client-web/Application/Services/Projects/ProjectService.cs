using System.Net;
using client_web.Application.Config.Http;
using client_web.Application.Services.Session;
using client_web.Models;

namespace client_web.Application.Services.Projects;

/// <summary>
/// Project + collaborator client.  Pulls the JWT from <see cref="ISessionManager"/>
/// on every call, so individual components don't need to thread the token through
/// their parameter lists. Mirrors <c>Layla.Desktop.Services.ProjectApiService</c>.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ApiClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(ApiClient client, ISessionManager session, ILogger<ProjectService> logger)
    {
        _client = client;
        _session = session;
        _logger = logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<IEnumerable<Project>> GetMyProjectsAsync()
    {
        try
        {
            var data = await _client.SendAsync<IEnumerable<Project>>(new APIRequest
            {
                Endpoint = "/api/projects",
                Method = HttpMethod.Get,
                Token = Token,
            });
            return data ?? Array.Empty<Project>();
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetMyProjectsAsync failed (HTTP {Status}).", ex.Status);
            return Array.Empty<Project>();
        }
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        try
        {
            var data = await _client.SendAsync<IEnumerable<Project>>(new APIRequest
            {
                Endpoint = "/api/projects/all",
                Method = HttpMethod.Get,
                Token = Token,
            });
            return data ?? Array.Empty<Project>();
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetAllProjectsAsync failed (HTTP {Status}).", ex.Status);
            return Array.Empty<Project>();
        }
    }

    public async Task<List<PublicProjectDto>> GetPublicProjectsAsync()
    {
        try
        {
            var data = await _client.SendAsync<List<PublicProjectDto>>(new APIRequest
            {
                Endpoint = "/api/projects/public",
                Method = HttpMethod.Get,
                Token = null,
            });
            return data ?? new List<PublicProjectDto>();
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetPublicProjectsAsync failed (HTTP {Status}).", ex.Status);
            return new List<PublicProjectDto>();
        }
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        try
        {
            return await _client.SendAsync<Project>(new APIRequest
            {
                Endpoint = $"/api/projects/{id}",
                Method = HttpMethod.Get,
                Token = Token,
            });
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetProjectByIdAsync({Id}) failed (HTTP {Status}).", id, ex.Status);
            return null;
        }
    }

    public async Task<Project?> CreateProjectAsync(CreateProjectRequest request)
    {
        try
        {
            return await _client.SendAsync<Project>(new APIRequest
            {
                Endpoint = "/api/projects",
                Method = HttpMethod.Post,
                Token = Token,
                Body = request,
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "CreateProjectAsync failed (HTTP {Status}).", ex.Status);
            return null;
        }
    }

    public async Task<Project?> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
    {
        try
        {
            return await _client.SendAsync<Project>(new APIRequest
            {
                Endpoint = $"/api/projects/{id}",
                Method = HttpMethod.Put,
                Token = Token,
                Body = request,
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "UpdateProjectAsync({Id}) failed (HTTP {Status}).", id, ex.Status);
            return null;
        }
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/projects/{id}",
                Method = HttpMethod.Delete,
                Token = Token,
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "DeleteProjectAsync({Id}) failed (HTTP {Status}).", id, ex.Status);
            return false;
        }
    }

    public async Task<bool> JoinPublicProjectAsync(Guid projectId)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/projects/{projectId}/join",
                Method = HttpMethod.Post,
                Token = Token,
            });
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "JoinPublicProjectAsync({Id}) failed (HTTP {Status}).", projectId, ex.Status);
            return false;
        }
    }
}
