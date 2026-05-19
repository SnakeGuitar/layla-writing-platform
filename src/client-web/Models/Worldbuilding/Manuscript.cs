using System;
using System.Collections.Generic;

namespace client_web.Models.Worldbuilding;

public class Manuscript
{
    public string ManuscriptId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<Chapter> Chapters { get; set; } = new List<Chapter>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
