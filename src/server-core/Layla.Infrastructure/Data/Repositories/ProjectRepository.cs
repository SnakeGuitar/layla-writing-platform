using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Layla.Infrastructure.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public ProjectRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task AddProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public async Task AddProjectRoleAsync(ProjectRole projectRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProjectRoles.AddAsync(projectRole, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var projects = await (from pr in _dbContext.ProjectRoles
                              where pr.AppUserId == userId && pr.Role == "OWNER"
                              join p in _dbContext.Projects on pr.ProjectId equals p.Id
                              select p).ToListAsync(cancellationToken);

        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task UpdateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Update(project);
        return Task.CompletedTask;
    }

    public Task DeleteProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Remove(project);
        return Task.CompletedTask;
    }

    public async Task<bool> UserHasRoleInProjectAsync(Guid projectId, string userId, string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProjectRoles
            .AnyAsync(pr => pr.ProjectId == projectId && pr.AppUserId == userId && pr.Role == role, cancellationToken);
    }

    public async Task<bool> UserHasAnyRoleInProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProjectRoles
            .AnyAsync(pr => pr.ProjectId == projectId && pr.AppUserId == userId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
