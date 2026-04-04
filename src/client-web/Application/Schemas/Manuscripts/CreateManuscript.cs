namespace client_web.Application.Schemas.Manuscripts;

/// <summary>Payload sent by the web client to create a new manuscript within a project.</summary>
public class CreateManuscript
{
    /// <summary>UUID of the project that will own the manuscript.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable title of the manuscript.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Zero-based display order among the project's manuscripts.</summary>
    public int Order { get; set; }
}
