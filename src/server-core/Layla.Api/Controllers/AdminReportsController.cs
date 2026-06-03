using Layla.Core.Constants;
using Layla.Core.Contracts.Admin;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

/// <summary>
/// Exposes system-level operational reports for administrators.
/// </summary>
[Route("api/admin/reports")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminReportsController : ApiControllerBase
{
    private readonly IAppUserService _appUserService;
    private readonly IProjectService _projectService;

    public AdminReportsController(IAppUserService appUserService, IProjectService projectService)
    {
        _appUserService = appUserService;
        _projectService = projectService;
    }

    /// <summary>
    /// Get aggregate system counters for the admin dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">System report generated from users and projects.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller does not have the Admin role.</response>
    [HttpGet("system")]
    [ProducesResponseType(typeof(SystemReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemReport(CancellationToken cancellationToken)
    {
        var usersResult = await _appUserService.GetAllAppUsersAsync(cancellationToken);
        if (!usersResult.IsSuccess)
            return RespondWithError(usersResult.ErrorCode);

        var projectsResult = await _projectService.GetAllProjectsAsync(cancellationToken);
        if (!projectsResult.IsSuccess)
            return RespondWithError(projectsResult.ErrorCode);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfDay = now.Date;
        var windowStart = startOfMonth.AddMonths(-11);

        var users = usersResult.Data!.ToList();
        var projects = projectsResult.Data!.ToList();
        var newUsersPerMonth = new int[12];

        foreach (var user in users)
        {
            var createdAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc);
            if (createdAt < windowStart)
                continue;

            var bucket = ((createdAt.Year - windowStart.Year) * 12) + createdAt.Month - windowStart.Month;
            if (bucket is >= 0 and < 12)
                newUsersPerMonth[bucket]++;
        }

        var report = new SystemReportDto
        {
            GeneratedAt = now,
            TotalUsers = users.Count,
            NewUsersThisMonth = users.Count(u => DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc) >= startOfMonth),
            BannedUsers = users.Count(u => u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow),
            TotalProjects = projects.Count,
            ProjectsModifiedToday = projects.Count(p => DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc) >= startOfDay),
            PublicProjects = projects.Count(p => p.IsPublic),
            NewUsersPerMonth = newUsersPerMonth
        };

        return Ok(report);
    }
}
