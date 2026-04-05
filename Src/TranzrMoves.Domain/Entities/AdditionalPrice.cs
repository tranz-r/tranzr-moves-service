using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class AdditionalPrice : IAuditable
{
    public Guid Id { get; set; }
    public AdditionalPriceType Type { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "GBP";

    public Instant EffectiveFrom { get; set; }
    public Instant? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; }
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}