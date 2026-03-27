using Layla.Core.Constants;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Layla.Infrastructure.Data.Repositories;

public class ProjectRepository : TransactionalRepository, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task AddProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        await DbContext.Projects.AddAsync(project, cancellationToken);
    }

    public async Task AddProjectRoleAsync(ProjectRole projectRole, CancellationToken cancellationToken = default)
    {
        await DbContext.ProjectRoles.AddAsync(projectRole, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects
            .AsNoTracking()
            .Include(p => p.Roles)
            .Where(p => p.Roles.Any(r => r.AppUserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task UpdateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        DbContext.Projects.Update(project);
        return Task.CompletedTask; // Update is synchronous in EF Core
    }

    public Task DeleteProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        DbContext.Projects.Remove(project);
        return Task.CompletedTask; // Remove is synchronous in EF Core
    }

    public async Task<bool> UserHasRoleInProjectAsync(Guid projectId, string userId, string role, CancellationToken cancellationToken = default)
    {
        var normalizedRole = ProjectRoles.Normalize(role) ?? role;
        return await DbContext.ProjectRoles
            .AnyAsync(pr => pr.ProjectId == projectId && pr.AppUserId == userId && pr.Role == normalizedRole, cancellationToken);
    }

    public async Task<bool> UserHasAnyRoleInProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.ProjectRoles
            .AnyAsync(pr => pr.ProjectId == projectId && pr.AppUserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetPublicProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects
            .AsNoTracking()
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProjectRole>> GetProjectCollaboratorsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbContext.ProjectRoles
            .Include(pr => pr.AppUser)
            .Where(pr => pr.ProjectId == projectId)
            .OrderBy(pr => pr.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectRole?> GetProjectRoleAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.ProjectRoles
            .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.AppUserId == userId, cancellationToken);
    }

    public Task RemoveProjectRoleAsync(ProjectRole role, CancellationToken cancellationToken = default)
    {
        DbContext.ProjectRoles.Remove(role);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
