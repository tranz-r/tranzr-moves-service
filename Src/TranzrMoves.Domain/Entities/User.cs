using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class User : IAuditable
{
    public Guid Id { get; set; }
    public Guid? SupabaseId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Role? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public Address? Address { get; set; }
    public ICollection<UserJob>? Jobs { get; set; } = [];
}