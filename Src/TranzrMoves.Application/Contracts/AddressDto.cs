namespace TranzrMoves.Application.Contracts;

public class AddressDto
{
    // public Guid Id { get; set; }
    // public Guid UserId { get; set; }
    public string? FullAddress { get; set; }
    public required string Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public required string PostCode { get; set; }
    public string? Country { get; set; }
    public bool HasElevator { get; set; }
    public int Floor { get; set; }
    
    // Extended Mapbox fields for complete address data
    public string? AddressNumber { get; set; }
    public string? Street { get; set; }
    public string? Neighborhood { get; set; }
    public string? District { get; set; }
    public string? Region { get; set; }
    public string? RegionCode { get; set; }
    public string? CountryCode { get; set; }
    public string? PlaceName { get; set; }
    public string? Accuracy { get; set; }
    public string? MapboxId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}