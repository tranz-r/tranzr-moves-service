using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class AdditionalPriceDto
{
    public Guid Id { get; set; }
    public AdditionalPriceType Type { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "GBP";

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; }
}