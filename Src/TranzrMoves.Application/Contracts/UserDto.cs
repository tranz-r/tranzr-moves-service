using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

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