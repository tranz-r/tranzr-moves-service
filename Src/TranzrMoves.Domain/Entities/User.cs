using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class User : IAuditable
{
    public Guid Id { get; set; }
    public Guid? SupabaseId { get; set; }
    public string? FullName { get; set; }
    public string? FirstName => FullName?.Split(' ').FirstOrDefault();
    public string? LastName => FullName?.Split(' ').LastOrDefault();
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Role? Role { get; set; }
    public Address? BillingAddress { get; set; }
    public ICollection<CustomerJob>? CustomerJobs { get; set; } = [];
    public ICollection<DriverJob>? DriverJobs { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}