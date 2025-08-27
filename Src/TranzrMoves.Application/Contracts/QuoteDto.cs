using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class QuoteDto
{
    public string? SessionId { get; set; } = string.Empty; // Maps to guestId
    public string? QuoteReference { get; set; } = string.Empty;
    public QuoteType? Type { get; set; }
    public VanType? VanType { get; set; }
    public long DriverCount { get; set; } = 1;
    public AddressDto? Origin { get; set; }
    public AddressDto? Destination { get; set; }
    public decimal? DistanceMiles { get; set; }
    public int NumberOfItemsToDismantle { get; set; }
    public int NumberOfItemsToAssemble { get; set; }
    public ScheduleDto? Schedule { get; set; }
    public PricingDto? Pricing { get; set; }
    public List<InventoryItemDto>? Items { get; set; } = [];
    public PaymentDto? Payment { get; set; }
    // Concurrency control using PostgreSQL xmin system column
    public uint Version { get; set; }
}

public class ScheduleDto
{
    public DateTime? DateISO { get; set; }
    public DateTime? DeliveryDateISO { get; set; }
    public int? Hours { get; set; }
    public bool? FlexibleTime { get; set; }
    public TimeSlot? TimeSlot { get; set; }
}

public class PricingDto
{
    public PricingTier? PricingTier { get; set; }
    public decimal? TotalCost { get; set; }
    // public decimal? PickUpDropOffPrice { get; set; }
}

public class PaymentDto
{
    public PaymentStatus Status { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal? DepositAmount { get; set; }
}
