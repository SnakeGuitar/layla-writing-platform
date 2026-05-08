namespace client_web.Models;

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Project.ProjectResponseDto</c> on server-core,
/// and the desktop client's <c>Layla.Desktop.Models.Project</c>.
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string LiteraryGenre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublic { get; set; }
    public bool IsAuthorActive { get; set; }
    public string? UserRole { get; set; }
}
