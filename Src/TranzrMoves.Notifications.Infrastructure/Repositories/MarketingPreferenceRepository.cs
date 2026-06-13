using Microsoft.EntityFrameworkCore;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure.Repositories;

public sealed class MarketingPreferenceRepository(NotificationsDbContext db) : IMarketingPreferenceRepository
{
    public Task<CustomerMarketingPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.CustomerMarketingPreferences
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CustomerMarketingPreference?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.CustomerMarketingPreferences
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public async Task AddAsync(CustomerMarketingPreference preference, CancellationToken cancellationToken)
    {
        await db.CustomerMarketingPreferences.AddAsync(preference, cancellationToken);
    }

    public async Task AddEventAsync(MarketingConsentEvent consentEvent, CancellationToken cancellationToken)
    {
        await db.MarketingConsentEvents.AddAsync(consentEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
