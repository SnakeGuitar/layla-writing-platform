namespace Layla.Core.Entities;

public class ProjectRole
{
    public Guid ProjectId { get; set; }
    /// <summary>Navigation property. Initialized by EF Core during query materialization.</summary>
    public Project Project { get; set; } = null!;

    public string AppUserId { get; set; } = string.Empty;
    /// <summary>Navigation property. Initialized by EF Core during query materialization.</summary>
    public AppUser AppUser { get; set; } = null!;

    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
