using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure.Repositories;

public interface IMarketingPreferenceRepository
{
    Task<CustomerMarketingPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerMarketingPreference?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(CustomerMarketingPreference preference, CancellationToken cancellationToken);

    Task AddEventAsync(MarketingConsentEvent consentEvent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
