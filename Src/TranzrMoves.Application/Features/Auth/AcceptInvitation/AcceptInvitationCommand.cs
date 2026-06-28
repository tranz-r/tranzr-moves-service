using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Auth.AcceptInvitation;

public sealed record AcceptInvitationCommand : IRequest<ErrorOr<AuthContextDto>>;

public sealed class AcceptInvitationCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IUserV2Repository userV2Repository,
    IBusinessUserRepository businessUserRepository,
    ILogger<AcceptInvitationCommandHandler> logger)
    : IRequestHandler<AcceptInvitationCommand, ErrorOr<AuthContextDto>>
{
    public async ValueTask<ErrorOr<AuthContextDto>> Handle(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var supabaseId = currentBusinessUserContext.SupabaseId;
        var email = currentBusinessUserContext.Email;

        if (supabaseId is null || string.IsNullOrWhiteSpace(email))
        {
            return Error.Unauthorized(
                code: "Auth.Unauthorized",
                description: "A valid authenticated session is required to accept an invitation.");
        }

        var user = await userV2Repository.GetUserByEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
        {
            return Error.NotFound(
                code: "Auth.InvitationNotFound",
                description: "No pending invitation was found for this account.");
        }

        // The invited email must not already be linked to a different Supabase identity.
        if (user.SupabaseId is not null && user.SupabaseId != supabaseId)
        {
            return Error.Conflict(
                code: "Auth.EmailLinkedToAnotherAccount",
                description: "This email is already linked to another auth account.");
        }

        var businessUser = await businessUserRepository.GetByUserIdGlobalAsync(user.Id, cancellationToken);
        if (businessUser is null)
        {
            return Error.NotFound(
                code: "Auth.InvitationNotFound",
                description: "No pending invitation was found for this account.");
        }

        switch (businessUser.Status)
        {
            case BusinessUserStatus.Suspended:
            case BusinessUserStatus.Deactivated:
                return Error.Forbidden(
                    code: "Auth.MembershipInactive",
                    description: "This membership is not eligible to access the Business Portal.");

            case BusinessUserStatus.Active:
                // Already accepted: idempotent success.
                return ToAuthContext(businessUser, user, email);

            case BusinessUserStatus.Invited:
            default:
                break;
        }

        var acceptResult = await businessUserRepository.AcceptInvitationAsync(
            user.Id,
            supabaseId.Value,
            businessUser.Id,
            cancellationToken);

        if (acceptResult.IsError)
        {
            return acceptResult.Errors;
        }

        logger.LogInformation(
            "Accepted invitation for business user {BusinessUserId} in business account {BusinessAccountId}",
            businessUser.Id,
            businessUser.BusinessAccountId);

        return ToAuthContext(acceptResult.Value, user, email);
    }

    private static AuthContextDto ToAuthContext(Domain.Entities.BusinessUser businessUser, UserV2 user, string email) =>
        new()
        {
            UserId = user.Id,
            BusinessUserId = businessUser.Id,
            BusinessAccountId = businessUser.BusinessAccountId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? email,
            Role = businessUser.Role,
        };
}
