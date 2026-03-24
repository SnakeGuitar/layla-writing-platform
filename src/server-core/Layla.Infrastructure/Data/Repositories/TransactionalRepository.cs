using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Layla.Infrastructure.Data.Repositories;

/// <summary>
/// Base class for repositories that require explicit transaction management.
/// Provides reusable transaction handling logic for derived repositories.
/// </summary>
public abstract class TransactionalRepository : ITransactionalRepository
{
    protected readonly ApplicationDbContext DbContext;
    protected IDbContextTransaction? CurrentTransaction;

    protected TransactionalRepository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction != null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Nested transactions are not supported.");

        CurrentTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            if (CurrentTransaction != null)
            {
                await CurrentTransaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (CurrentTransaction != null)
            {
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (CurrentTransaction != null)
            {
                await CurrentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (CurrentTransaction != null)
            {
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }
    }
}
