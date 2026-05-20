using client_web.Models;

namespace client_web.Application.Services.Admin;

/// <summary>
/// Read-only endpoints used by the administration pages. Pulls the bearer
/// token from <see cref="Session.ISessionManager"/> like the rest of the
/// services in this project, so callers don't have to thread it through.
/// </summary>
public interface IAdminService
{
    Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default);
    Task<bool> BanUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default);
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
}

/// <summary>Projection of <c>Layla.Core.Contracts.AppUser.UserResponseDto</c>.</summary>
public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsBanned { get; set; }
}

/// <summary>Aggregate counters derived client-side from the server-core lists.</summary>
public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int BannedUsers { get; set; }
    public int TotalProjects { get; set; }
    public int ProjectsModifiedToday { get; set; }
    public int PublicProjects { get; set; }
    /// <summary>Number of new users registered per month for the last 12 months (oldest → newest).</summary>
    public int[] NewUsersPerMonth { get; set; } = new int[12];
}
