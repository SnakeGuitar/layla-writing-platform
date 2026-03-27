using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.Project;

public record UpdateProjectRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Synopsis { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LiteraryGenre { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? CoverImageUrl { get; set; }

    public bool IsPublic { get; set; }
}
