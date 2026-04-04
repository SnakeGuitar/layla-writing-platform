namespace client_web.Application.Schemas.Wikis;

public class CreateWikiPage
{
    public string WikiId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}