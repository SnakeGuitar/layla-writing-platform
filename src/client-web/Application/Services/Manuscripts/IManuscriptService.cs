using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using client_web.Models.Worldbuilding;

namespace client_web.Application.Services.Manuscripts;

public interface IManuscriptService
{
    Task<List<Manuscript>?> GetManuscriptsByProjectAsync(Guid projectId);
    Task<Manuscript?> GetManuscriptAsync(Guid projectId, string manuscriptId);
    Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order);
    Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order);
    Task<bool> DeleteManuscriptAsync(Guid projectId, string manuscriptId);
    
    Task<Chapter?> GetChapterAsync(Guid projectId, string manuscriptId, Guid chapterId);
    Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order);
    Task<Chapter?> UpdateChapterAsync(Guid projectId, string manuscriptId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null);
    Task<bool> DeleteChapterAsync(Guid projectId, string manuscriptId, Guid chapterId);
    
    Task<List<Layla.Client.Shared.Models.ChapterVersionMeta>?> GetChapterVersionsAsync(Guid projectId, string manuscriptId, Guid chapterId);
    Task<Layla.Client.Shared.Models.ChapterVersionFull?> GetChapterVersionAsync(Guid projectId, string manuscriptId, Guid chapterId, string versionId);
    Task<bool> RestoreChapterVersionAsync(Guid projectId, string manuscriptId, Guid chapterId, string versionId);
}
