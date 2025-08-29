using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

/// <summary>Pricing for a movers crew size + service level.</summary>
public sealed class RateCard : IAuditable
{
    public Guid Id { get; set; }
    public int Movers { get; set; }                     // 1, 2, 3
    public ServiceLevel ServiceLevel { get; set; }      // Standard / Premium

    public int BaseBlockHours { get; set; }
    public decimal BaseBlockPrice { get; set; }         // numeric(10,2)
    public decimal HourlyRateAfter { get; set; }        // numeric(10,2)

    public string CurrencyCode { get; set; } = "GBP";

    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveTo { get; set; }    // null = open-ended
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";

    // Optimistic concurrency
    public uint Version { get; set; }
}