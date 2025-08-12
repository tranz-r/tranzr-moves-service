using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class Job : IAuditable
{
    public Guid Id { get; set; }
    public VanType VanType { get; set; }
    public long DriverCount { get; set; }
    public long DistanceMiles { get; set; }
    public required string QuoteId { get; set; }
    public required Address Origin { get; set; }
    public required Address Destination { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public string? ReceiptUrl { get; set; }
    public required PricingTier PricingTier { get; set; }
    public required DateTime CollectionDate  { get; set; }
    public Cost Cost { get; set; }
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<CustomerJob>? CustomerJobs { get; set; } = [];
    public List<DriverJob>? DriverJobs { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}