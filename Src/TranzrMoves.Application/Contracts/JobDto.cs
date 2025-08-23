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

public class UserDto
{
    // public Guid Id { get; set; }
    public Guid? SupabaseId { get; set; }
    public string? FullName { get; set; }
    public string? FirstName => FullName?.Split(' ').FirstOrDefault();
    public string? LastName => FullName?.Split(' ').LastOrDefault();
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Role? Role { get; set; }
    public AddressDto? BillingAddress { get; set; }
}

public class InventoryItemDto
{
    // public Guid Id { get; set; }
    // public Guid JobId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Depth { get; set; }
    public int? Quantity { get; set; }
}

public class AddressDto
{
    // public Guid Id { get; set; }
    // public Guid UserId { get; set; }
    public required string Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public required string PostCode { get; set; }
    public string? Country { get; set; }
    public bool HasElevator { get; set; }
    public int Floor { get; set; }
}

public class CostDto
{
    public Guid JobId { get; set; }
    public long BaseVan { get; set; }
    public double Distance { get; set; }
    public long Floor { get; set; }
    public long ElevatorAdjustment  { get; set; }
    public long Driver  { get; set; }
    public double TierAdjustment  { get; set; }
    public double Total  { get; set; }
}