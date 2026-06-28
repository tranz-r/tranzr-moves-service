namespace TranzrMoves.Domain.Interfaces;

/// <summary>
/// Holds the Business Account (tenant) for the current scope. Resolved lazily
/// during authorization and consumed by the global query filter. Kept free of
/// other dependencies so the DbContext can depend on it without a cycle.
/// </summary>
public interface ITenantProvider
{
    Guid? BusinessAccountId { get; }

    bool HasTenant { get; }

    void SetBusinessAccount(Guid businessAccountId);
}
