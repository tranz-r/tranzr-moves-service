using NodaTime;

namespace TranzrMoves.Notifications.Infrastructure.Entities;

public sealed class CustomerMarketingPreference
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }

    public bool EmailMarketingEnabled { get; set; }

    public bool SmsMarketingEnabled { get; set; }

    public Instant? EmailMarketingConsentedAt { get; set; }

    public Instant? SmsMarketingConsentedAt { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant UpdatedAt { get; set; }

    public ICollection<MarketingConsentEvent> Events { get; set; } = [];
}
