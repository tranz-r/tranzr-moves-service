namespace TranzrMoves.Notifications.Contracts;

public sealed record ApplyMarketingPreferencesRequest(
    string Email,
    bool EmailMarketingEnabled,
    bool SmsMarketingEnabled,
    MarketingConsentSource Source,
    Guid? CustomerId = null,
    string? IpAddress = null,
    string? UserAgent = null);
