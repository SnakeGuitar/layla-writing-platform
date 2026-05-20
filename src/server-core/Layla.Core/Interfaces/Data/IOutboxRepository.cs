using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Layla.Core.Entities;

namespace Layla.Core.Interfaces.Data;

public interface IOutboxRepository
{
    Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 20, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
