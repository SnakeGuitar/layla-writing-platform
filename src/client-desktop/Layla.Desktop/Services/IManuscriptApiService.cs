using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
    public interface IManuscriptApiService
    {
        Task<Manuscript> GetManuscriptAsync(Guid projectId);
        Task<Chapter> GetChapterAsync(Guid projectId, Guid chapterId);
        Task<Chapter> CreateChapterAsync(Guid projectId, string title, string content, int order);
        Task<Chapter> UpdateChapterAsync(Guid projectId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null);
    }
}
