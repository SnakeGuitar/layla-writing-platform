using Layla.Core.Common;
using Layla.Core.Constants;
using Layla.Core.Contracts.Project;
using Layla.Core.Entities;
using Layla.Core.Events;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Queue;
using Layla.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Layla.Core.Services;

/// <summary>
/// Business logic for project (manuscript) management in the Layla collaborative writing platform.
///
/// Responsibilities:
/// - CRUD operations for projects and project roles (collaborators)
/// - Access control: verifies user ownership/membership before mutations
/// - Event publishing: notifies the worldbuilding service (Node.js) when projects are created
/// - Transactional consistency: ensures projects and their initial owner role are created atomically
/// - Public project discovery: lists all public projects for the reading feed
///
/// Architecture:
/// - Uses IProjectRepository for SQL queries (via EF Core)
/// - Uses IAppUserRepository to resolve user details (email → ID) for invitations
/// - Uses IEventPublisher (inherits from IEventBus) to publish both domain events and integration events
/// - Inherits from BaseService&lt;ProjectService&gt; for centralized exception handling and logging
///
/// All public methods return Result&lt;T&gt; to encapsulate success/failure without throwing exceptions.
/// Exception handling is delegated to BaseService.ExecuteAsync() which logs and maps errors to ErrorCode.
/// </summary>
public class ProjectService : BaseService<ProjectService>, IProjectService
{
    private const string ExchangeName = MessagingConstants.WorldbuildingExchange;
    private const string ProjectCreatedRoutingKey = MessagingConstants.RoutingKeys.ProjectCreated;

    private readonly IProjectRepository _projectRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IEventPublisher _eventPublisher;

    public ProjectService(
        IProjectRepository projectRepository,
        IAppUserRepository appUserRepository,
        IEventPublisher eventPublisher,
        ILogger<ProjectService> logger)
        : base(logger)
    {
        _projectRepository = projectRepository;
        _appUserRepository = appUserRepository;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Creates a new project owned by the authenticated user.
    ///
    /// This operation is transactional:
    /// 1. Inserts a new Project row
    /// 2. Inserts a ProjectRole row with role=OWNER and the creator's user ID
    /// 3. Commits the transaction atomically
    /// 4. Publishes ProjectCreatedEvent to RabbitMQ (asynchronously, after commit)
    ///
    /// The creator is automatically assigned the OWNER role and can invite other collaborators.
    /// The OWNER is the only user who can delete the project or remove collaborators.
    ///
    /// If the transaction fails, all changes are rolled back. If event publishing fails,
    /// a warning is logged but the operation is still considered successful (eventual consistency).
    /// </summary>
    /// <param name="request">Project metadata (title, synopsis, genre, cover image, privacy setting).</param>
    /// <param name="userId">The ID of the user creating the project (extracted from JWT claim).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// Success result with ProjectResponseDto if the project was created.
    /// Failure result with DatabaseError if a database constraint violated or connection failed.
    /// Failure result with InternalError for unexpected exceptions.
    /// </returns>
    public async Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        var project = BuildProject(request);
        var projectRole = BuildOwnerRole(project.Id, userId);

        try
        {
            // ExecuteInTransactionAsync wraps the work inside CreateExecutionStrategy().ExecuteAsync(),
            // which is required when SqlServerRetryingExecutionStrategy is active.
            await _projectRepository.ExecuteInTransactionAsync(async ct =>
            {
                await _projectRepository.AddProjectAsync(project, ct);
                await _projectRepository.AddProjectRoleAsync(projectRole, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create project for user {UserId}", userId);
            return Result<ProjectResponseDto>.Failure(MapException(ex));
        }

        await PublishProjectCreatedEventsAsync(project, userId, cancellationToken);

        Logger.LogInformation("Project {ProjectId} created successfully by user {UserId}", project.Id, userId);
        return Result<ProjectResponseDto>.Success(MapToResponseDto(project, ProjectRoles.Owner));
    }

    public Task<Result<IEnumerable<ProjectResponseDto>>> GetUserProjectsAsync(string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var projects = await _projectRepository.GetProjectsByUserIdAsync(userId, cancellationToken);
            var dtos = projects
                .Select(p => MapToResponseDto(p, p.Roles?.FirstOrDefault(r => r.AppUserId == userId)?.Role ?? string.Empty))
                .ToList();
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);
        }, "Failed to retrieve projects for user {UserId}", userId);

    public Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<ProjectResponseDto>.Failure(ErrorCode.Forbidden);

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectResponseDto>.Failure(ErrorCode.ProjectNotFound);

            ApplyProjectUpdate(project, request);

            await _projectRepository.UpdateProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Project {ProjectId} updated by user {UserId}", projectId, userId);
            return Result<ProjectResponseDto>.Success(MapToResponseDto(project, ProjectRoles.Owner));
        }, "Failed to update project {ProjectId} for user {UserId}", projectId, userId);

    public Task<Result<bool>> DeleteProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<bool>.Failure(ErrorCode.Forbidden);

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<bool>.Failure(ErrorCode.ProjectNotFound);

            await _projectRepository.DeleteProjectAsync(project, cancellationToken);
            await _projectRepository.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Project {ProjectId} deleted by user {UserId}", projectId, userId);
            return Result<bool>.Success(true);
        }, "Failed to delete project {ProjectId} for user {UserId}", projectId, userId);

    public Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectResponseDto>.Failure(ErrorCode.ProjectNotFound);

            return Result<ProjectResponseDto>.Success(MapToResponseDto(project));
        }, "Failed to retrieve project {ProjectId}", projectId);

    public async Task<bool> UserHasAccessAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);

    public Task<Result<IEnumerable<ProjectResponseDto>>> GetAllProjectsAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var projects = await _projectRepository.GetAllProjectsAsync(cancellationToken);
            var dtos = projects.Select(p => MapToResponseDto(p)).ToList();
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);
        }, "Failed to retrieve all projects");

    public Task<Result<IEnumerable<ProjectResponseDto>>> GetPublicProjectsAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var projects = await _projectRepository.GetPublicProjectsAsync(cancellationToken);
            var dtos = projects.Select(p => MapToResponseDto(p)).ToList();
            return Result<IEnumerable<ProjectResponseDto>>.Success(dtos);
        }, "Failed to retrieve public projects");

    public Task<Result<CollaboratorResponseDto>> JoinPublicProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.InvalidInput, "Invalid user ID.");

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.ProjectNotFound);

            if (!project.IsPublic)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.InvalidInput, "Project is not public.");

            // Optimistic check for the common path: avoids hitting the duplicate-key
            // exception path under normal traffic. The DB-level catch below closes
            // the race window between this check and SaveChangesAsync.
            var alreadyMember = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);
            if (alreadyMember)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);

            var projectRole = new ProjectRole
            {
                ProjectId = projectId,
                AppUserId = userId,
                Role = ProjectRoles.Reader,
                AssignedAt = DateTime.UtcNow
            };

            try
            {
                await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
                await _projectRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Composite PK (ProjectId, AppUserId) on ProjectRoles guarantees
                // that a concurrent join request will fail at SaveChanges. Translate
                // it into the same error the optimistic check would have produced.
                Logger.LogInformation(ex, "Race detected joining project {ProjectId} for user {UserId} — translating to AlreadyMember.", projectId, userId);
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);
            }

            var userResult = await _appUserRepository.GetAppUserByIdAsync(userGuid, cancellationToken);
            if (!userResult.IsSuccess || userResult.Data == null)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.UserNotFound);

            return Result<CollaboratorResponseDto>.Success(
                MapToCollaboratorDto(userResult.Data, ProjectRoles.Reader, projectRole.AssignedAt));
        }, "Failed to join public project {ProjectId} for user {UserId}", projectId, userId);

    public Task<Result<CollaboratorResponseDto>> InviteCollaboratorAsync(Guid projectId, InviteCollaboratorRequestDto request, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var hasRole = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!hasRole)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.Forbidden);

            var targetUserResult = await _appUserRepository.GetAppUserByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
            if (!targetUserResult.IsSuccess || targetUserResult.Data == null)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.UserNotFound);

            var targetUser = targetUserResult.Data;

            var alreadyMember = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, targetUser.Id, cancellationToken);
            if (alreadyMember)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);

            var normalizedRole = ProjectRoles.Normalize(request.Role);
            if (normalizedRole == null || normalizedRole == ProjectRoles.Owner)
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.InvalidRole);

            var projectRole = new ProjectRole
            {
                ProjectId = projectId,
                AppUserId = targetUser.Id,
                Role = normalizedRole,
                AssignedAt = DateTime.UtcNow
            };

            try
            {
                await _projectRepository.AddProjectRoleAsync(projectRole, cancellationToken);
                await _projectRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                Logger.LogInformation(ex, "Race detected inviting user {TargetUserId} to project {ProjectId} — translating to AlreadyMember.", targetUser.Id, projectId);
                return Result<CollaboratorResponseDto>.Failure(ErrorCode.AlreadyMember);
            }

            return Result<CollaboratorResponseDto>.Success(
                MapToCollaboratorDto(targetUser, normalizedRole, projectRole.AssignedAt));
        }, "Failed to invite collaborator to project {ProjectId}", projectId);

    public Task<Result<IEnumerable<CollaboratorResponseDto>>> GetCollaboratorsAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var hasAccess = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, cancellationToken);
            if (!hasAccess)
                return Result<IEnumerable<CollaboratorResponseDto>>.Failure(ErrorCode.Forbidden);

            var roles = await _projectRepository.GetProjectCollaboratorsAsync(projectId, cancellationToken);
            var dtos = roles.Select(r => new CollaboratorResponseDto
            {
                UserId = r.AppUserId,
                DisplayName = r.AppUser?.DisplayName ?? "Unknown User",
                Email = r.AppUser?.Email,
                Role = r.Role,
                AssignedAt = r.AssignedAt
            }).ToList();

            return Result<IEnumerable<CollaboratorResponseDto>>.Success(dtos);
        }, "Failed to get collaborators for project {ProjectId}", projectId);

    public Task<Result<bool>> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
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
        }, "Failed to remove collaborator from project {ProjectId}", projectId);

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task PublishProjectCreatedEventsAsync(Project project, string userId, CancellationToken cancellationToken)
    {
        var domainEvent = new ProjectCreatedEvent
        {
            ProjectId = project.Id,
            OwnerUserId = userId,
            Title = project.Title,
            CreatedAt = project.CreatedAt
        };

        if (!await _eventPublisher.PublishAsync(domainEvent, cancellationToken))
            Logger.LogWarning("Domain event not published for project {ProjectId}. Downstream services may be out of sync.", project.Id);

        var integrationEvent = new Layla.Core.IntegrationEvents.ProjectCreatedEvent
        {
            ProjectId = project.Id.ToString(),
            OwnerId = userId,
            Title = project.Title,
            CreatedAt = project.CreatedAt
        };

        if (!_eventPublisher.Publish(integrationEvent, exchangeName: ExchangeName,
                                    routingKey: ProjectCreatedRoutingKey))
            Logger.LogWarning("Integration event not published for project {ProjectId}. Node.js worldbuilding service may be out of sync.", project.Id);
    }

    private static Project BuildProject(CreateProjectRequestDto request) => new()
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

    private static ProjectRole BuildOwnerRole(Guid projectId, string userId) => new()
    {
        ProjectId = projectId,
        AppUserId = userId,
        Role = ProjectRoles.Owner,
        AssignedAt = DateTime.UtcNow
    };

    private static void ApplyProjectUpdate(Project project, UpdateProjectRequestDto request)
    {
        project.Title = request.Title;
        project.Synopsis = request.Synopsis;
        project.LiteraryGenre = request.LiteraryGenre;
        project.CoverImageUrl = request.CoverImageUrl;
        project.IsPublic = request.IsPublic;
        project.UpdatedAt = DateTime.UtcNow;
    }

    private static CollaboratorResponseDto MapToCollaboratorDto(AppUser user, string role, DateTime assignedAt) => new()
    {
        UserId = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Role = role,
        AssignedAt = assignedAt
    };

    private static ProjectResponseDto MapToResponseDto(Project project, string userRole = "") => new()
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
