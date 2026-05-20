using System.Net;
using System.Text.Json.Serialization;
using client_web.Application.Config.Http;
using client_web.Application.Services.Projects;
using client_web.Application.Services.Session;
using client_web.Models;

namespace client_web.Application.Services.Admin;

/// <summary>
/// Adapter on top of <see cref="ApiClient"/> for the admin pages.
/// All endpoints are gated by <c>[Authorize(Roles = "Admin")]</c> on
/// server-core, so the caller is expected to already hold an admin JWT.
/// </summary>
public class AdminService : IAdminService
{
    private readonly ApiClient _client;
    private readonly ISessionManager _session;
    private readonly IProjectService _projects;
    private readonly ILogger<AdminService> _logger;

    public AdminService(ApiClient client, ISessionManager session, IProjectService projects, ILogger<AdminService> logger)
    {
        _client = client;
        _session = session;
        _projects = projects;
        _logger = logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var raw = await _client.SendAsync<List<UserDto>>(new APIRequest
            {
                Endpoint = "/api/users",
                Method = HttpMethod.Get,
                Token = Token,
            }, ct);

            return (raw ?? new List<UserDto>())
                .Select(u => new AdminUser
                {
                    Id = Guid.TryParse(u.Id, out var g) ? g : Guid.Empty,
                    Email = u.Email ?? string.Empty,
                    DisplayName = u.DisplayName ?? u.UserName ?? string.Empty,
                    Bio = u.Bio,
                    CreatedAt = u.CreatedAt,
                    IsBanned = u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow,
                })
                .ToList();
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetUsersAsync failed (HTTP {Status}).", ex.Status);
            return Array.Empty<AdminUser>();
        }
    }

    public async Task<bool> BanUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/users/{userId}/ban",
                Method = HttpMethod.Post,
                Token = Token,
            }, ct);
            return true;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "BanUserAsync({Id}) failed (HTTP {Status}).", userId, ex.Status);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            await _client.SendAsync<object>(new APIRequest
            {
                Endpoint = $"/api/users/{userId}",
                Method = HttpMethod.Delete,
                Token = Token,
            }, ct);
            return true;
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "DeleteUserAsync({Id}) failed (HTTP {Status}).", userId, ex.Status);
            return false;
        }
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        // Server-core doesn't expose aggregated counters yet, so we derive
        // them client-side from the two list endpoints. Cheap for the admin
        // dashboard's expected scale and avoids a server change.
        var usersTask = GetUsersAsync(ct);
        var projectsTask = _projects.GetAllProjectsAsync();
        await Task.WhenAll(usersTask, projectsTask);

        var users = await usersTask;
        var projects = (await projectsTask).ToList();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfDay = now.Date.ToUniversalTime();

        // 12-month rolling window, oldest bucket first.
        var perMonth = new int[12];
        var windowStart = startOfMonth.AddMonths(-11);
        foreach (var u in users)
        {
            var created = DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc);
            if (created < windowStart) continue;
            var bucket = ((created.Year - windowStart.Year) * 12) + (created.Month - windowStart.Month);
            if (bucket is >= 0 and < 12) perMonth[bucket]++;
        }

        return new DashboardStats
        {
            TotalUsers = users.Count,
            NewUsersThisMonth = users.Count(u => u.CreatedAt >= startOfMonth),
            BannedUsers = users.Count(u => u.IsBanned),
            TotalProjects = projects.Count,
            ProjectsModifiedToday = projects.Count(p => p.UpdatedAt >= startOfDay),
            PublicProjects = projects.Count(p => p.IsPublic),
            NewUsersPerMonth = perMonth,
        };
    }

    /// <summary>
    /// Shape returned by <c>GET /api/users</c>. Extends <c>UserResponseDto</c>
    /// with the lockout fields exposed by ASP.NET Identity so we can flag
    /// banned accounts in the table.
    /// </summary>
    private sealed class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("lockoutEnd")] public DateTimeOffset? LockoutEnd { get; set; }
    }
}
