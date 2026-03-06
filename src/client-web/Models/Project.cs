namespace client_web.Models;

public class Project
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Synopsis { get; set; }
    public required string LiteraryGenre { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegación
    public ICollection<ProjectRole> Roles { get; set; } = [];
}
