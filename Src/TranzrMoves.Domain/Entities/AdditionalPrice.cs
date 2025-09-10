using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class AdditionalPrice : IAuditable
{
    public Guid Id { get; set; }
    public AdditionalPriceType Type { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "GBP";

    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}