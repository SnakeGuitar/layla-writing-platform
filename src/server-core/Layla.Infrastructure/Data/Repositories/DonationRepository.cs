using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Layla.Infrastructure.Data.Repositories;

public class DonationRepository : IDonationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DonationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddDonationAsync(Donation donation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Donations.AddAsync(donation, cancellationToken);
    }

    public async Task<Donation?> GetDonationByPayPalOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Donations
            .Include(d => d.DonorUser)
            .FirstOrDefaultAsync(d => d.PayPalOrderId == orderId, cancellationToken);
    }

    public async Task<IEnumerable<Donation>> GetProjectDonationsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Donations
            .AsNoTracking()
            .Include(d => d.DonorUser)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
