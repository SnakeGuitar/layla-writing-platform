namespace client_web.Application.Schemas.Wikis;

public class UpdateWikiRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}