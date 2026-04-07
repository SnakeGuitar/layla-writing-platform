using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.Project;

public record InviteCollaboratorRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "READER";
}
