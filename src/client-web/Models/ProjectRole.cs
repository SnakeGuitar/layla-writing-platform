namespace client_web.Models;

public class ProjectRole
{
    public Guid ProjectId { get; set; }
    public required string AppUserId { get; set; }
    public required string Role { get; set; }
    public DateTime AssignedAt { get; set; }

    // Navegación
    public Project Project { get; set; } = null!;
    public AppUser AppUser { get; set; } = null!;
}
