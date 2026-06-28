using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Authentication;

public sealed class TenantProvider : ITenantProvider
{
    public Guid? BusinessAccountId { get; private set; }

    public bool HasTenant => BusinessAccountId is not null;

    public void SetBusinessAccount(Guid businessAccountId) => BusinessAccountId = businessAccountId;
}
