namespace client_web.Application.Schemas.Wikis;

public class UpdateWikiPage
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}