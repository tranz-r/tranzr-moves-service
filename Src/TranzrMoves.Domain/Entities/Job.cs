using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class Job : IAuditable
{
    public Guid Id { get; set; }
    public required string QuoteId { get; set; }
    public required Address Origin { get; set; }
    public required Address Destination { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public string? ReceiptUrl { get; set; }
    public required PricingTier PricingTier { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}