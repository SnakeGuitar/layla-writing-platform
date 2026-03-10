using Layla.Core.Common;
using Layla.Core.Contracts.Project;
using Layla.Core.Entities;
using Layla.Core.Events;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Messaging;
using Layla.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Layla.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IProjectRepository projectRepository,
        IEventPublisher eventPublisher,
        IEventBus eventBus,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _eventPublisher = eventPublisher;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<Project>> CreateProjectAsync(CreateProjectRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        await _projectRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Synopsis = request.Synopsis,
                LiteraryGenre = request.LiteraryGenre,
                CoverImageUrl = request.CoverImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var projectRole = new ProjectRole
            {
                ProjectId = project.Id,
                AppUserId = userId,
                Role = "OWNER",
                AssignedAt = DateTime.UtcNow
            };

            await _projectRepository.AddProjectAsync(project, cancellationToken);
            await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
            
            await _projectRepository.SaveChangesAsync(cancellationToken);

            var projectCreatedEvent = new ProjectCreatedEvent
            {
                ProjectId = project.Id,
                OwnerUserId = userId,
                Title = project.Title,
                CreatedAt = project.CreatedAt
            };

            await _eventPublisher.PublishAsync(projectCreatedEvent, cancellationToken);

            var integrationEvent = new Layla.Core.IntegrationEvents.ProjectCreatedEvent
            {
                ProjectId = project.Id.ToString(),
                OwnerId = userId,
                Title = project.Title,
                CreatedAt = project.CreatedAt
            };

            _eventBus.Publish(integrationEvent, exchangeName: "worldbuilding.events", routingKey: "project.created");

            await _projectRepository.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Project {ProjectId} created successfully by user {UserId}", project.Id, userId);

            return Result<Project>.Success(project);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _projectRepository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create project for user {UserId}", userId);
            return Result<Project>.Failure("An error occurred while creating the project.");
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetUserProjectsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetProjectsByUserIdAsync(userId, cancellationToken);
            return Result<IEnumerable<Project>>.Success(projects);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _logger.LogError(ex, "Failed to retrieve projects for user {UserId}", userId);
            return Result<IEnumerable<Project>>.Failure("An error occurred while retrieving projects.");
        }
    }

    public async Task<Result<Project>> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, "OWNER", cancellationToken);
            if (!hasRole)
            {
                return Result<Project>.Failure("Unauthorized.");
            }

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                return Result<Project>.Failure("Project not found.");
            }

            project.Title = request.Title;
            project.Synopsis = request.Synopsis;
            project.LiteraryGenre = request.LiteraryGenre;
            project.CoverImageUrl = request.CoverImageUrl;
            project.UpdatedAt = DateTime.UtcNow;

            await _projectRepository.UpdateProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Project {ProjectId} updated successfully by user {UserId}", projectId, userId);

            return Result<Project>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project {ProjectId} for user {UserId}", projectId, userId);
            return Result<Project>.Failure("An error occurred while updating the project.");
        }
    }

    public async Task<Result<bool>> DeleteProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, "OWNER", cancellationToken);
            if (!hasRole)
            {
                return Result<bool>.Failure("Unauthorized.");
            }

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                return Result<bool>.Failure("Project not found.");
            }

            await _projectRepository.DeleteProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Project {ProjectId} deleted successfully by user {UserId}", projectId, userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project {ProjectId} for user {UserId}", projectId, userId);
            return Result<bool>.Failure("An error occurred while deleting the project.");
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetAllProjectsAsync(cancellationToken);
            return Result<IEnumerable<Project>>.Success(projects);

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all projects");
            return Result<IEnumerable<Project>>.Failure("An error occurred while retrieving projects.");
        }
}
