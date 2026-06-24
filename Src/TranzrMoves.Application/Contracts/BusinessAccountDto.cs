using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public sealed class BusinessAccountDto
{
    public Guid BusinessAccountId { get; init; }
    public required string BusinessName { get; init; }
    public string? TradingName { get; init; }
    public required string BusinessEmail { get; init; }
    public required string BusinessPhone { get; init; }
    public string? CompanyRegistrationNumber { get; init; }
    public string? VatNumber { get; init; }
    public BusinessAccountStatus Status { get; init; }
    public required AddressDto BillingAddress { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant ModifiedAt { get; init; }
}

public sealed class BusinessOwnerSignupDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
}

public sealed class RegisterBusinessAccountResponse
{
    public Guid BusinessAccountId { get; init; }
    public Guid BusinessUserId { get; init; }
    public Guid UserId { get; init; }
    public BusinessUserRole Role { get; init; }
    public BusinessUserStatus Status { get; init; }
}
