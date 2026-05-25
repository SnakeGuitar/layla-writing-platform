using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.Project;

public class ChangeCollaboratorRoleRequestDto
{
    /// <summary>New role for the collaborator. Accepted values: EDITOR, READER.</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}
