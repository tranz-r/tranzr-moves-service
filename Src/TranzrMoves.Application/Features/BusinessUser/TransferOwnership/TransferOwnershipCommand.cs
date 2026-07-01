using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.TransferOwnership;

public sealed record TransferOwnershipCommand(Guid BusinessUserId)
    : IRequest<ErrorOr<TransferOwnershipResponse>>;

public sealed class TransferOwnershipCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository)
    : IRequestHandler<TransferOwnershipCommand, ErrorOr<TransferOwnershipResponse>>
{
    public async ValueTask<ErrorOr<TransferOwnershipResponse>> Handle(
        TransferOwnershipCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        // BR-009: only an Owner may transfer ownership.
        if (caller.Role != BusinessUserRole.Owner)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "Only the account owner can transfer ownership.");
        }

        // Cannot transfer ownership to self.
        if (caller.Id == command.BusinessUserId)
        {
            return Error.Validation(
                code: "BusinessUser.SelfTransfer",
                description: "You cannot transfer ownership to yourself.");
        }

        var target = await businessUserRepository.GetByIdAsync(command.BusinessUserId, cancellationToken);
        if (target is null)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        // AC-016: target must belong to the caller's business account.
        if (target.BusinessAccountId != caller.BusinessAccountId)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        // Target must be Active (cannot transfer to suspended/deactivated/invited/revoked).
        if (target.Status != BusinessUserStatus.Active)
        {
            return Error.Validation(
                code: "BusinessUser.TargetNotActive",
                description: "Ownership can only be transferred to an active member.");
        }

        var result = await businessUserRepository.TransferOwnershipAsync(
            caller.Id,
            command.BusinessUserId,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        return new TransferOwnershipResponse
        {
            PreviousOwnerRole = BusinessUserRole.Admin,
            NewOwnerRole = BusinessUserRole.Owner,
        };
    }
}
