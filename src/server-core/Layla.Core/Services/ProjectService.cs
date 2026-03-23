using Layla.Core.Common;
using Layla.Core.Constants;
using Layla.Core.Contracts.Project;
using Layla.Core.Entities;
using Layla.Core.Events;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Messaging;
using Layla.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Layla.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IProjectRepository projectRepository,
        IAppUserRepository appUserRepository,
        IEventPublisher eventPublisher,
        IEventBus eventBus,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _appUserRepository = appUserRepository;
        _eventPublisher = eventPublisher;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectRequestDto request, string userId, CancellationToken cancellationToken = default)
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
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var projectRole = new ProjectRole
            {
                ProjectId = project.Id,
                AppUserId = userId,
                Role = ProjectRoles.Owner,
                AssignedAt = DateTime.UtcNow
            };

            await _projectRepository.AddProjectAsync(project, cancellationToken);
            await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
            // DO NOT call SaveChangesAsync here — let CommitTransactionAsync handle it
            await _projectRepository.CommitTransactionAsync(cancellationToken);

            // Publish events AFTER successful commit (outbox pattern)
            await PublishProjectCreatedEventsAsync(project, userId, cancellationToken);

            _logger.LogInformation("Project {ProjectId} created successfully by user {UserId}", project.Id, userId);

            return Result<ProjectResponseDto>.Success(MapToResponseDto(project, ProjectRoles.Owner));
        }
        catch (Exception ex)
        {
            await _projectRepository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create project for user {UserId}", userId);
            return Result<ProjectResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    private async Task PublishProjectCreatedEventsAsync(Project project, string userId, CancellationToken cancellationToken)
    {
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
    }

    public async Task<Result<IEnumerable<ProjectResponseDto>>> GetUserProjectsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetProjectsByUserIdAsync(userId, cancellationToken);
            var dtos = projects.Select(p => MapToResponseDto(p, p.Roles.FirstOrDefault(r => r.AppUserId == userId)?.Role ?? string.Empty));
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve projects for user {UserId}", userId);
            return Result<IEnumerable<ProjectResponseDto>>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<ProjectResponseDto>.Failure(ErrorCode.Forbidden);

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectResponseDto>.Failure(ErrorCode.ProjectNotFound);

            project.Title = request.Title;
            project.Synopsis = request.Synopsis;
            project.LiteraryGenre = request.LiteraryGenre;
            project.CoverImageUrl = request.CoverImageUrl;
            project.IsPublic = request.IsPublic;
            project.UpdatedAt = DateTime.UtcNow;

            await _projectRepository.UpdateProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Project {ProjectId} updated successfully by user {UserId}", projectId, userId);
            return Result<ProjectResponseDto>.Success(MapToResponseDto(project, ProjectRoles.Owner));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project {ProjectId} for user {UserId}", projectId, userId);
            return Result<ProjectResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<bool>> DeleteProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<bool>.Failure(ErrorCode.Forbidden);

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<bool>.Failure(ErrorCode.ProjectNotFound);

            await _projectRepository.DeleteProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Project {ProjectId} deleted successfully by user {UserId}", projectId, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project {ProjectId} for user {UserId}", projectId, userId);
            return Result<bool>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectResponseDto>.Failure(ErrorCode.ProjectNotFound);

            return Result<ProjectResponseDto>.Success(MapToResponseDto(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve project {ProjectId}", projectId);
            return Result<ProjectResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<bool> UserHasAccessAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);
    }

    public async Task<Result<IEnumerable<ProjectResponseDto>>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetAllProjectsAsync(cancellationToken);
            var dtos = projects.Select(p => MapToResponseDto(p));
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all projects");
            return Result<IEnumerable<ProjectResponseDto>>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<IEnumerable<ProjectResponseDto>>> GetPublicProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetPublicProjectsAsync(cancellationToken);
            var dtos = projects.Select(p => MapToResponseDto(p));
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve public projects");
            return Result<IEnumerable<ProjectResponseDto>>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<CollaboratorResponseDto>> JoinPublicProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.ProjectNotFound);

            if (!project.IsPublic)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.InvalidInput, "Project is not public.");

            var existingRole = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);
            if (existingRole)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);

            if (!Guid.TryParse(userId, out var userGuid))
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.InvalidInput, "Invalid user ID.");

            var projectRole = new ProjectRole
            {
                ProjectId = projectId,
                AppUserId = userId,
                Role = ProjectRoles.Reader,
                AssignedAt = DateTime.UtcNow
            };

            await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            var userResult = await _appUserRepository.GetAppUserByIdAsync(userGuid, cancellationToken);
            var user = userResult.Data;

            return Result<CollaboratorResponseDto>.Success(new CollaboratorResponseDto
            {
                UserId = userId,
                DisplayName = user?.DisplayName,
                Email = user?.Email,
                Role = ProjectRoles.Reader,
                AssignedAt = projectRole.AssignedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join public project {ProjectId} for user {UserId}", projectId, userId);
            return Result<CollaboratorResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<CollaboratorResponseDto>> InviteCollaboratorAsync(Guid projectId, InviteCollaboratorRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.Forbidden);

            var targetUserResult = await _appUserRepository.GetAppUserByEmailAsync(request.Email, cancellationToken);
            if (!targetUserResult.IsSuccess || targetUserResult.Data == null)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.UserNotFound);

            var targetUser = targetUserResult.Data;

            var alreadyMember = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, targetUser.Id, cancellationToken);
            if (alreadyMember)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);

            var role = request.Role == ProjectRoles.Editor ? ProjectRoles.Editor : ProjectRoles.Reader;

            var projectRole = new ProjectRole
            {
                ProjectId = projectId,
                AppUserId = targetUser.Id,
                Role = role,
                AssignedAt = DateTime.UtcNow
            };

            await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            return Result<CollaboratorResponseDto>.Success(new CollaboratorResponseDto
            {
                UserId = targetUser.Id,
                DisplayName = targetUser.DisplayName,
                Email = targetUser.Email,
                Role = role,
                AssignedAt = projectRole.AssignedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite collaborator to project {ProjectId}", projectId);
            return Result<CollaboratorResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<IEnumerable<CollaboratorResponseDto>>> GetCollaboratorsAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasAccess = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);
            if (!hasAccess)
                return Result<IEnumerable<CollaboratorResponseDto>>.Failure(ErrorCode.Unauthorized);

            var roles = await _projectRepository.GetProjectCollaboratorsAsync(projectId, cancellationToken);
            var dtos = roles.Select(r => new CollaboratorResponseDto
            {
                UserId = r.AppUserId,
                DisplayName = r.AppUser?.DisplayName,
                Email = r.AppUser?.Email,
                Role = r.Role,
                AssignedAt = r.AssignedAt
            });

            return Result<IEnumerable<CollaboratorResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collaborators for project {ProjectId}", projectId);
            return Result<IEnumerable<CollaboratorResponseDto>>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<bool>> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var isOwner = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!isOwner)
                return Result<bool>.Failure(ErrorCode.Forbidden);

            var role = await _projectRepository.GetProjectRoleAsync(projectId, collaboratorUserId, cancellationToken);
            if (role == null)
                return Result<bool>.Failure(ErrorCode.CollaboratorNotFound);

            if (role.Role == ProjectRoles.Owner)
                return Result<bool>.Failure(ErrorCode.InvalidInput, "Cannot remove the project owner.");

            await _projectRepository.RemoveProjectRoleAsync(role, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove collaborator from project {ProjectId}", projectId);
            return Result<bool>.Failure(ErrorCode.InternalError);
        }
    }

    private static ProjectResponseDto MapToResponseDto(Project project, string userRole = "")
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Title = project.Title,
            Synopsis = project.Synopsis,
            LiteraryGenre = project.LiteraryGenre,
            CoverImageUrl = project.CoverImageUrl,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            IsPublic = project.IsPublic,
            UserRole = userRole
        };
    }
}
