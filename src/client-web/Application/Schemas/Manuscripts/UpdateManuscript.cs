namespace client_web.Application.Schemas.Manuscripts;

/// <summary>Payload sent by the web client to rename or reorder an existing manuscript.</summary>
public class UpdateManuscript
{
    /// <summary>New title. Pass <c>null</c> to leave the title unchanged.</summary>
    public string? Title { get; set; }

    /// <summary>New display order. Pass <c>null</c> to leave the order unchanged.</summary>
    public int? Order { get; set; }
}
