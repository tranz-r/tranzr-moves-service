using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class BusinessAccount : IAuditable
{
    public Guid Id { get; set; }
    public required string BusinessName { get; set; }
    public string? TradingName { get; set; }
    public required string BusinessEmail { get; set; }
    public required string BusinessPhone { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public BusinessAccountStatus Status { get; set; } = BusinessAccountStatus.Active;
    public required BillingAddress BillingAddress { get; set; }

    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";

    public ICollection<BusinessUser> BusinessUsers { get; set; } = [];
}
