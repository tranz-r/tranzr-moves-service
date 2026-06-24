using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IBusinessAccountRepository
{
    Task<BusinessAccount?> GetByIdAsync(Guid businessAccountId, CancellationToken cancellationToken);

    Task<ErrorOr<BusinessAccount>> UpdateAsync(BusinessAccount businessAccount, CancellationToken cancellationToken);

    Task<ErrorOr<BusinessAccount>> SuspendAsync(Guid businessAccountId, CancellationToken cancellationToken);

    Task<ErrorOr<BusinessAccount>> ActivateAsync(Guid businessAccountId, CancellationToken cancellationToken);

    Task<ErrorOr<RegisterBusinessAccountResult>> RegisterAsync(
        UserV2 user,
        BusinessAccount businessAccount,
        BusinessUser businessUser,
        CancellationToken cancellationToken);
}

public sealed record RegisterBusinessAccountResult(
    Guid UserId,
    Guid BusinessAccountId,
    Guid BusinessUserId,
    BusinessUserRole Role,
    BusinessUserStatus Status);
