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
        if (CurrentTransaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            await CurrentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await CurrentTransaction!.DisposeAsync();
            CurrentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction == null)
            return;

        try
        {
            await CurrentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await CurrentTransaction!.DisposeAsync();
            CurrentTransaction = null;
        }
    }
}
