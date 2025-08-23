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
    public DateTimeOffset? ExpiresUtc { get; set; }
    public string ETag { get; set; } = string.Empty;
    
    // Core Quote Data
    public VanType VanType { get; set; }
    public long DriverCount { get; set; }
    public long DistanceMiles { get; set; }
    public string QuoteId { get; set; } = string.Empty;
    
    // Addresses
    public Address? Origin { get; set; }
    public Address? Destination { get; set; }
    
    // Schedule
    public DateTime? CollectionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int? Hours { get; set; }
    public bool? FlexibleTime { get; set; }
    public string? TimeSlot { get; set; } // morning, afternoon, evening
    
    // Pricing
    public PricingTier? PricingTier { get; set; }
    public decimal? TotalCost { get; set; }
    public Cost? Cost { get; set; }
    
    // Items
    public List<InventoryItem> InventoryItems { get; set; } = [];
    
    // Customer
    public CustomerInfo? Customer { get; set; }
    
    // Payment
    public PaymentStatus? PaymentStatus { get; set; }
    public string? ReceiptUrl { get; set; }
    
    // Relationships
    public List<CustomerJob>? CustomerJobs { get; set; } = [];
    public List<DriverJob>? DriverJobs { get; set; } = [];
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}

public class CustomerInfo
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? BillingAddress { get; set; }
    public CustomerPreferences? Preferences { get; set; }
}

public class CustomerPreferences
{
    public VanType? PreferredVanSize { get; set; }
    public bool? DefaultFlexibleTime { get; set; }
    public EmergencyContact? EmergencyContact { get; set; }
}

public class EmergencyContact
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Relationship { get; set; }
}
