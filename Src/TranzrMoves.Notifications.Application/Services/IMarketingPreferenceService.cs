using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Notifications.Application.Services;

public interface IMarketingPreferenceService
{
    Task<MarketingPreferenceDto> ApplyPreferencesAsync(
        ApplyMarketingPreferencesRequest request,
        CancellationToken cancellationToken);

    Task<MarketingPreferenceDto?> GetByIdAsync(Guid prefId, CancellationToken cancellationToken);

    Task<bool> IsChannelEnabledAsync(
        string email,
        MarketingConsentChannel channel,
        CancellationToken cancellationToken);

    Task<Guid> GetOrCreatePreferenceIdAsync(
        string email,
        Guid? customerId,
        CancellationToken cancellationToken);
}
