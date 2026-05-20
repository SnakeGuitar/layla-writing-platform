using System;
using System.Threading.Tasks;
using client_web.Models.Worldbuilding;

namespace client_web.Application.Services.Graph;

public interface IGraphService
{
    Task<GraphResult?> GetGraphAsync(Guid projectId, string? entityType = null);
    Task<bool> CreateRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId, string type, string? label = null);
    Task<bool> DeleteRelationshipAsync(Guid projectId, string sourceEntityId, string targetEntityId);
}
