namespace Layla.Desktop.Models
{
    public class UpdateProjectRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Synopsis { get; set; } = string.Empty;
        public string LiteraryGenre { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public bool IsPublic { get; set; }
    }
}
