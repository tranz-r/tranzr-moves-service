namespace TranzrMoves.Domain.Entities;

public class Address
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public string? County { get; set; }
    public required string PostCode { get; set; }
    public string? Country { get; set; }
}