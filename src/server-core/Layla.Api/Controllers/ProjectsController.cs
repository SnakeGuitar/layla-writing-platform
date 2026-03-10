using Layla.Api.Extensions;
using Layla.Core.Contracts.Project;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Error = "User ID not found in token." });
        }

        var result = await _projectService.CreateProjectAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }

        var project = result.Data!;
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Error = "User ID not found in token." });
        }

        var result = await _projectService.GetUserProjectsAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllProjects(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetAllProjectsAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public IActionResult GetProjectById(Guid id)
    {
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Error = "User ID not found in token." });
        }

        var result = await _projectService.UpdateProjectAsync(id, request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "Unauthorized.")
            {
                return Forbid();
            }
            if (result.Error == "Project not found.")
            {
                return NotFound(new { Error = result.Error });
            }
            return BadRequest(new { Error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Error = "User ID not found in token." });
        }

        var result = await _projectService.DeleteProjectAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "Unauthorized.")
            {
                return Forbid();
            }
            if (result.Error == "Project not found.")
            {
                return NotFound(new { Error = result.Error });
            }
            return BadRequest(new { Error = result.Error });
        }

        return NoContent();
    }
}