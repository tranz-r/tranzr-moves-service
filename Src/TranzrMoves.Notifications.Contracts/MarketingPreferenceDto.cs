namespace TranzrMoves.Notifications.Contracts;

public sealed record MarketingPreferenceDto(
    Guid Id,
    string Email,
    bool EmailMarketingEnabled,
    bool SmsMarketingEnabled);
