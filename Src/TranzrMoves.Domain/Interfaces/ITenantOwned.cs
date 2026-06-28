namespace TranzrMoves.Domain.Interfaces;

/// <summary>
/// Marker for entities that belong to a single Business Account (tenant).
/// Entities implementing this are automatically scoped to the current tenant
/// via a global query filter in <c>TranzrMovesDbContext</c>.
/// </summary>
public interface ITenantOwned
{
    Guid BusinessAccountId { get; }
}
