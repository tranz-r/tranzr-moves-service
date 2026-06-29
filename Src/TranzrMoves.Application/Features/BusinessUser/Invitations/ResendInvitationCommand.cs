using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Invitations;

public sealed record ResendInvitationCommand(Guid BusinessUserId) : IRequest<ErrorOr<InvitationActionResponse>>;

public sealed class ResendInvitationCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    ISupabaseAuthAdminService supabaseAuthAdminService,
    ITimeService timeService,
    ILogger<ResendInvitationCommandHandler> logger)
    : IRequestHandler<ResendInvitationCommand, ErrorOr<InvitationActionResponse>>
{
    // Matches the Supabase invite-link lifetime (24h).
    private static readonly Duration InvitationLifetime = Duration.FromHours(24);

    public async ValueTask<ErrorOr<InvitationActionResponse>> Handle(
        ResendInvitationCommand command,
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

        // Resend applies to pending or already-expired invitations.
        if (target.Status != BusinessUserStatus.Invited)
        {
            return Error.Conflict(
                code: "BusinessUser.NotPending",
                description: "Only pending invitations can be resent.");
        }

        var email = target.User?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Failure(
                code: "BusinessUser.MissingEmail",
                description: "The invitation has no email address to resend to.");
        }

        var resendResult = await supabaseAuthAdminService.ResendInvitationAsync(
            new SupabaseInviteUserRequest(
                email,
                target.User?.FirstName,
                target.User?.LastName,
                target.Role.ToString(),
                caller.BusinessAccountId),
            cancellationToken);

        if (resendResult.IsError)
        {
            return resendResult.Errors;
        }

        var expiresAt = timeService.Now() + InvitationLifetime;

        var updateResult = await businessUserRepository.UpdateInvitationAsync(
            target.Id,
            target.Role,
            BusinessUserStatus.Invited,
            expiresAt,
            cancellationToken);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        logger.LogInformation(
            "Resent invitation {BusinessUserId} in business account {BusinessAccountId}",
            command.BusinessUserId,
            caller.BusinessAccountId);

        return new InvitationActionResponse
        {
            Status = BusinessUserStatus.Invited,
            ExpiresAtUtc = expiresAt,
        };
    }
}
