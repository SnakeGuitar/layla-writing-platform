using Layla.Core.Contracts.Project;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

/// <summary>
/// Manages writing projects and their collaborators.
/// All endpoints require a valid JWT Bearer token unless stated otherwise.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    /// <summary>Initialises the controller with the project service.</summary>
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Create a new writing project owned by the authenticated user.
    /// </summary>
    /// <remarks>The caller is automatically assigned the <c>OWNER</c> role and a
    /// <c>ProjectCreatedEvent</c> is published to RabbitMQ for the worldbuilding service.</remarks>
    /// <param name="request">Project details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Project created. Location header points to the new resource.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _projectService.CreateProjectAsync(request, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        var project = result.Data!;
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    /// <summary>
    /// Get all projects where the authenticated user holds any role.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of projects.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetUserProjectsAsync(CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all public projects. No authentication required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of public projects ordered by last update descending.</response>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicProjects(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetPublicProjectsAsync(cancellationToken);
        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all projects in the system (Admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">All projects.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller does not have the Admin role.</response>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllProjects(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetAllProjectsAsync(cancellationToken);
        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a single project by ID.
    /// </summary>
    /// <remarks>
    /// Accessible when the project is public OR when the caller holds any role in it.
    /// </remarks>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Project details including the caller's role.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Project is private and the caller is not a member.</response>
    /// <response code="404">Project not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        if (!result.Data!.IsPublic)
        {
            var isMember = await _projectService.UserHasAccessAsync(id, CurrentUserId, cancellationToken);
            if (!isMember)
                return Forbid();
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Update a project's metadata (OWNER only).
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="request">Updated fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Updated project.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the project OWNER.</response>
    /// <response code="404">Project not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _projectService.UpdateProjectAsync(id, request, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a project and all associated data (OWNER only).
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Project deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the project OWNER.</response>
    /// <response code="404">Project not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.DeleteProjectAsync(id, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return NoContent();
    }

    /// <summary>
    /// Join a public project as a READER.
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Joined successfully. Returns updated role assignment.</response>
    /// <response code="400">Project is private or user is already a member.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> JoinPublicProject(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.JoinPublicProjectAsync(id, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Invite a user to the project by email address (OWNER only).
    /// </summary>
    /// <remarks>The invited user is assigned <c>READER</c> by default; pass <c>role: "EDITOR"</c>
    /// to grant editing rights.</remarks>
    /// <param name="id">Project ID.</param>
    /// <param name="request">Invite payload — email address and optional role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Collaborator added. Returns the new role assignment.</response>
    /// <response code="400">User not found or already a member.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the project OWNER.</response>
    [HttpPost("{id:guid}/collaborators")]
    [ProducesResponseType(typeof(CollaboratorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCollaborator(Guid id, [FromBody] InviteCollaboratorRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _projectService.InviteCollaboratorAsync(id, request, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// List all collaborators of a project.
    /// </summary>
    /// <param name="id">Project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of collaborators with their assigned roles.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not a member of the project.</response>
    [HttpGet("{id:guid}/collaborators")]
    [ProducesResponseType(typeof(IEnumerable<CollaboratorResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCollaborators(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetCollaboratorsAsync(id, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Remove a collaborator from a project (OWNER only).
    /// </summary>
    /// <remarks>The project OWNER cannot be removed via this endpoint.</remarks>
    /// <param name="id">Project ID.</param>
    /// <param name="collaboratorUserId">ID of the user to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Collaborator removed.</response>
    /// <response code="400">Cannot remove the OWNER or user is not a member.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the project OWNER.</response>
    [HttpDelete("{id:guid}/collaborators/{collaboratorUserId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveCollaborator(Guid id, string collaboratorUserId, CancellationToken cancellationToken)
    {
        var result = await _projectService.RemoveCollaboratorAsync(id, collaboratorUserId, CurrentUserId, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return NoContent();
    }
}
