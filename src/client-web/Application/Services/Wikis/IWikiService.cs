using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using client_web.Models.Worldbuilding;

namespace client_web.Application.Services.Wikis;

public interface IWikiService
{
    Task<List<WikiEntry>?> GetEntriesAsync(Guid projectId, string? entityType = null);
    Task<WikiEntry?> GetEntryAsync(Guid projectId, string entityId);
    Task<WikiEntry?> CreateEntryAsync(Guid projectId, string name, string entityType, string? description = null, List<string>? tags = null);
    Task<WikiEntry?> UpdateEntryAsync(Guid projectId, string entityId, string? name = null, string? entityType = null, string? description = null, List<string>? tags = null);
    Task<bool> DeleteEntryAsync(Guid projectId, string entityId);
    Task<List<AppearanceRecord>?> GetEntityAppearancesAsync(Guid projectId, string entityId);
}
