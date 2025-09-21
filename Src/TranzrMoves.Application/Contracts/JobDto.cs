using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public record JobDto
{
    public Guid Id { get; set; }
    public required string QuoteId { get; set; }
    public VanType VanType { get; set; }
    public required AddressDto Origin { get; set; }
    public required AddressDto Destination { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public string? ReceiptUrl { get; set; }
    public required PricingTier PricingTier { get; set; }
    public DateTime CollectionDate { get; set; }
    public long DriverCount { get; set; }
    public long DistanceMiles { get; set; }
    public CostDto Cost { get; set; }
    public UserDto User { get; set; }
    public List<InventoryItemDto> InventoryItems { get; set; } = [];
}