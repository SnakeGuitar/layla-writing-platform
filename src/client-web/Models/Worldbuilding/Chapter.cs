using System;
using System.Collections.Generic;

namespace client_web.Models.Worldbuilding;

public class Chapter
{
    public Guid ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<Mention> Mentions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
