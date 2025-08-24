using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public enum QuoteType
{
    Send,
    Receive,
    Removals
}

public class Quote : IAuditable
{
    public Guid Id { get; set; }
    
    // Session Management
    public string SessionId { get; set; } = string.Empty; // Maps to guestId
    public QuoteType Type { get; set; } // send, receive, removals
    
    //Origin and Destination addresses
    public Address? Origin { get; set; }
    public Address? Destination { get; set; }
    
    public decimal? DistanceMiles { get; set; }
    public int NumberOfItemsToDismantle { get; set; }
    public int NumberOfItemsToAssemble { get; set; }
    
    // Quote-specific data only
    public VanType VanType { get; set; }
    public long DriverCount { get; set; }
    public string QuoteReference { get; set; } = string.Empty;
    
    // Schedule
    public DateTime? CollectionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int? Hours { get; set; }
    public bool? FlexibleTime { get; set; }
    public TimeSlot? TimeSlot { get; set; } // morning, afternoon, evening
    
    // Pricing
    public PricingTier? PricingTier { get; set; }
    public decimal? TotalCost { get; set; }
    
    // Items
    public List<InventoryItem> InventoryItems { get; set; } = [];
    
    // Payment
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? ReceiptUrl { get; set; }
    
    // Note: Customer information is stored in User entity, not directly in Quote
    
    // Relationships
    public List<CustomerQuote>? CustomerQuotes { get; set; } = [];
    public List<DriverQuote>? DriverQuotes { get; set; } = [];
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}