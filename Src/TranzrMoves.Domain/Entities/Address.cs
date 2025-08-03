using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class Address : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public string? County { get; set; }
    public required string PostCode { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public User User { get; set; } = null!;
}