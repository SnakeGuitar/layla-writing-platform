namespace client_web.Models;

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Project.CreateProjectRequestDto</c> on server-core.
/// </summary>
public class CreateProjectRequest
{
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string LiteraryGenre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public bool IsPublic { get; set; }
}
