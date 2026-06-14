using Layla.Core.Entities;

namespace Layla.Core.Interfaces.Data;

public interface IDonationRepository
{
    Task AddDonationAsync(Donation donation, CancellationToken cancellationToken = default);
    Task<Donation?> GetDonationByPayPalOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Donation>> GetProjectDonationsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
