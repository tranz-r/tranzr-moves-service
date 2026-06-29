using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Invitations;

public sealed record RevokeInvitationCommand(Guid BusinessUserId) : IRequest<ErrorOr<InvitationActionResponse>>;

public sealed class RevokeInvitationCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    ILogger<RevokeInvitationCommandHandler> logger)
    : IRequestHandler<RevokeInvitationCommand, ErrorOr<InvitationActionResponse>>
{
    public async ValueTask<ErrorOr<InvitationActionResponse>> Handle(
        RevokeInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        // Tenant-scoped: a cross-tenant id resolves to null (AC-016).
        var target = await businessUserRepository.GetByIdAsync(command.BusinessUserId, cancellationToken);
        if (target is null || target.BusinessAccountId != caller.BusinessAccountId)
        {
            return Error.NotFound(
                code: "BusinessUser.InvitationNotFound",
                description: "Invitation not found.");
        }

        if (target.Status != BusinessUserStatus.Invited)
        {
            return Error.Conflict(
                code: "BusinessUser.NotPending",
                description: "Only pending invitations can be revoked.");
        }

        var result = await businessUserRepository.UpdateStatusAsync(
            command.BusinessUserId,
            BusinessUserStatus.Revoked,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        logger.LogInformation(
            "Revoked invitation {BusinessUserId} in business account {BusinessAccountId}",
            command.BusinessUserId,
            caller.BusinessAccountId);

        return new InvitationActionResponse { Status = BusinessUserStatus.Revoked };
    }
}
