using client_web.Application.Config.Http;
using client_web.Application.Schemas.Profile;
using client_web.Application.Services.Session;
using client_web.Models;

namespace client_web.Application.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly ApiClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ApiClient client, ISessionManager session, ILogger<ProfileService> logger)
    {
        _client = client;
        _session = session;
        _logger = logger;
    }

    public async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(_session.CurrentUserId, out var userId)) return null;

        try
        {
            return await _client.SendAsync<UserProfile>(new APIRequest
            {
                Endpoint = $"/api/users/{userId}",
                Method = HttpMethod.Get,
                Token = _session.CurrentToken,
            }, ct);
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetCurrentProfileAsync failed (HTTP {Status}).", ex.Status);
            return null;
        }
    }

    public async Task<(bool Success, UserProfile? Profile, string? Error)> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        if (!Guid.TryParse(_session.CurrentUserId, out var userId))
            return (false, null, "No hay una sesion activa.");

        try
        {
            var updated = await _client.SendAsync<UserProfile>(new APIRequest
            {
                Endpoint = $"/api/users/{userId}",
                Method = HttpMethod.Put,
                Token = _session.CurrentToken,
                Body = request,
            }, ct);

            await _session.UpdateProfileAsync(updated.DisplayName, updated.AvatarUrl, updated.Bio);
            return (true, updated, null);
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "UpdateProfileAsync failed (HTTP {Status}).", ex.Status);
            return (false, null, ex.Message);
        }
    }
}
