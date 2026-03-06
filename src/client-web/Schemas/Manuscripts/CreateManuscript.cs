namespace client_web.Schemas.Manuscripts;

public class CreateManuscript
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}