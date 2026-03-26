using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.AppUser;

public record UpdateAppUserRequestDto
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }
}
