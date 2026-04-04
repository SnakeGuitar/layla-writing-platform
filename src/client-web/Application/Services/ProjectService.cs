using client_web.Application.Services.Http;

namespace client_web.Services;

public class ProjectService
{
    private readonly ApiClient _api;

    public ProjectService(ApiClient api)
    {
        _api = api;
    }

    public async Task<IEnumerable<ProjectResponse>> GetUserProjectsAsync(string token)
    {
        return await _api.SendAsync<IEnumerable<ProjectResponse>>(new APIRequest
        {
            Endpoint = "/api/projects",
            Method = HttpMethod.Get,
            Token = token
        });
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllProjectsAsync(string token)
    {
        return await _api.SendAsync<IEnumerable<ProjectResponse>>(new APIRequest
        {
            Endpoint = "/api/projects/all",
            Method = HttpMethod.Get,
            Token = token
        });
    }
}

public class ProjectResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string LiteraryGenre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}
