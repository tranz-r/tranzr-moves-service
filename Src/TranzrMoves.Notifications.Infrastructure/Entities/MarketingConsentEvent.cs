using NodaTime;
using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Notifications.Infrastructure.Entities;

public sealed class MarketingConsentEvent
{
    public Guid Id { get; set; }

    public Guid CustomerMarketingPreferenceId { get; set; }

    public string Email { get; set; } = string.Empty;

    public MarketingConsentChannel Channel { get; set; }

    public MarketingConsentEventType EventType { get; set; }

    public MarketingConsentSource Source { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Instant OccurredAt { get; set; }

    public CustomerMarketingPreference? Preference { get; set; }
}
