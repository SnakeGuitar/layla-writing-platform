using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Layla.Desktop.Models
{
    public class Manuscript
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
