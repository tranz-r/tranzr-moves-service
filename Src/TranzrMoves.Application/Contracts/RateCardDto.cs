using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class RateCardDto
{
    public Guid Id { get; set; }
    public int Movers { get; set; }
    public ServiceLevel ServiceLevel { get; set; }

    public int BaseBlockHours { get; set; }
    public decimal BaseBlockPrice { get; set; }
    public decimal HourlyRateAfter { get; set; }

    public string CurrencyCode { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Optimistic concurrency
    public uint Version { get; set; }
}