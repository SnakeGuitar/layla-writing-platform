using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Layla.Infrastructure.Data.Repositories;

public class OutboxRepository : TransactionalRepository, IOutboxRepository
{
    public OutboxRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await DbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 20, CancellationToken cancellationToken = default)
    {
        return await DbContext.OutboxMessages
            .Where(om => !om.Processed)
            .OrderBy(om => om.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
