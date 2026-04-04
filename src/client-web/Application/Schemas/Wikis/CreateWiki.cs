namespace client_web.Application.Schemas.Wikis;

public class CreateWiki
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}