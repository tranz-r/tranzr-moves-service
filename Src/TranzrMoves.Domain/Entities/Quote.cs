using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class Job : IAuditable
{
    public Guid Id { get; set; }
    public VanType VanType { get; set; }
    public long DriverCount { get; set; }
    public long DistanceMiles { get; set; }
    public required string QuoteId { get; set; }
    public Address? Origin { get; set; }
    public Address? Destination { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? ReceiptUrl { get; set; }
    public PricingTier? PricingTier { get; set; }
    public DateTime? CollectionDate  { get; set; }
    public Cost? Cost { get; set; }
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<CustomerJob>? CustomerJobs { get; set; } = [];
    public List<DriverJob>? DriverJobs { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}