using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Invite;

public sealed record InviteBusinessUserCommand(
    string? FirstName,
    string? LastName,
    string Email,
    BusinessUserRole Role) : IRequest<ErrorOr<InviteBusinessUserResponse>>;

public sealed class InviteBusinessUserCommandValidator : AbstractValidator<InviteBusinessUserCommand>
{
    public InviteBusinessUserCommandValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(BusinessUserRole.Owner)
            .WithMessage("The Owner role cannot be assigned through an invitation.");
    }
}

public sealed class InviteBusinessUserCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IUserV2Repository userV2Repository,
    IBusinessUserRepository businessUserRepository,
    ISupabaseAuthAdminService supabaseAuthAdminService,
    ITimeService timeService,
    ILogger<InviteBusinessUserCommandHandler> logger)
    : IRequestHandler<InviteBusinessUserCommand, ErrorOr<InviteBusinessUserResponse>>
{
    // Matches the Supabase invite-link lifetime (hosted Supabase caps email link/OTP expiry at 24h).
    private static readonly Duration InvitationLifetime = Duration.FromHours(24);

    public async ValueTask<ErrorOr<InviteBusinessUserResponse>> Handle(
        InviteBusinessUserCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        var email = command.Email.Trim();
        var firstName = string.IsNullOrWhiteSpace(command.FirstName) ? null : command.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(command.LastName) ? null : command.LastName.Trim();
        var now = timeService.Now();
        var expiresAt = now + InvitationLifetime;

        var existingUser = await userV2Repository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            var existingMembership = await businessUserRepository.GetByUserIdGlobalAsync(
                existingUser.Id,
                cancellationToken);

            if (existingMembership is not null)
            {
                // BR-001: a user can belong to only one Business Account.
                if (existingMembership.BusinessAccountId != caller.BusinessAccountId)
                {
                    return Error.Conflict(
                        code: "BusinessUser.AlreadyMember",
                        description: "This user already belongs to a business account.");
                }

                return existingMembership.Status switch
                {
                    // BR-011/AC-015: only one pending invitation per email. Expired ones use resend too.
                    BusinessUserStatus.Invited => Error.Conflict(
                        code: "BusinessUser.PendingInvitationExists",
                        description: "A pending invitation already exists for this email. Use resend to send a new link."),

                    // BR-010/AC-014: cannot re-invite an active/suspended/deactivated member.
                    BusinessUserStatus.Active or BusinessUserStatus.Suspended or BusinessUserStatus.Deactivated =>
                        Error.Conflict(
                            code: "BusinessUser.AlreadyMember",
                            description: "This user already belongs to this business account."),

                    // A previously revoked invitation can be re-issued by reusing the row.
                    BusinessUserStatus.Revoked => await ReinviteRevokedAsync(
                        existingMembership, existingUser, command.Role, firstName, lastName, expiresAt, caller, cancellationToken),

                    _ => Error.Conflict(
                        code: "BusinessUser.AlreadyMember",
                        description: "This user already belongs to this business account."),
                };
            }
        }

        var inviteResult = await supabaseAuthAdminService.InviteUserByEmailAsync(
            new SupabaseInviteUserRequest(
                email,
                firstName,
                lastName,
                command.Role.ToString(),
                caller.BusinessAccountId),
            cancellationToken);

        if (inviteResult.IsError)
        {
            return inviteResult.Errors;
        }

        // The Supabase id is linked to the UserV2 when the invitee first authenticates
        // (handled by the Business Authentication feature), not at invite time.
        UserV2 user;
        bool userIsNew;
        if (existingUser is null)
        {
            user = new UserV2
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
            };
            userIsNew = true;
        }
        else
        {
            user = existingUser;
            userIsNew = false;
        }

        var businessUser = new Domain.Entities.BusinessUser
        {
            Id = Guid.NewGuid(),
            BusinessAccountId = caller.BusinessAccountId,
            UserId = user.Id,
            Role = command.Role,
            Status = BusinessUserStatus.Invited,
            CreatedByBusinessUserId = caller.Id,
            InvitationExpiresAt = expiresAt,
        };

        var persistResult = await businessUserRepository.InviteAsync(
            user,
            userIsNew,
            businessUser,
            cancellationToken);

        if (persistResult.IsError)
        {
            return persistResult.Errors;
        }

        logger.LogInformation(
            "Invited business user {BusinessUserId} ({Role}) to business account {BusinessAccountId}",
            businessUser.Id,
            businessUser.Role,
            caller.BusinessAccountId);

        return new InviteBusinessUserResponse
        {
            BusinessUserId = businessUser.Id,
            Status = BusinessUserStatus.Invited,
            ExpiresAtUtc = expiresAt,
        };
    }

    private async Task<ErrorOr<InviteBusinessUserResponse>> ReinviteRevokedAsync(
        Domain.Entities.BusinessUser membership,
        UserV2 user,
        BusinessUserRole role,
        string? firstName,
        string? lastName,
        Instant expiresAt,
        Domain.Entities.BusinessUser caller,
        CancellationToken cancellationToken)
    {
        var inviteResult = await supabaseAuthAdminService.InviteUserByEmailAsync(
            new SupabaseInviteUserRequest(
                user.Email ?? string.Empty,
                firstName ?? user.FirstName,
                lastName ?? user.LastName,
                role.ToString(),
                caller.BusinessAccountId),
            cancellationToken);

        if (inviteResult.IsError)
        {
            return inviteResult.Errors;
        }

        var updateResult = await businessUserRepository.UpdateInvitationAsync(
            membership.Id,
            role,
            BusinessUserStatus.Invited,
            expiresAt,
            cancellationToken);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        logger.LogInformation(
            "Re-invited previously revoked business user {BusinessUserId} ({Role}) in business account {BusinessAccountId}",
            membership.Id,
            role,
            caller.BusinessAccountId);

        return new InviteBusinessUserResponse
        {
            BusinessUserId = membership.Id,
            Status = BusinessUserStatus.Invited,
            ExpiresAtUtc = expiresAt,
        };
    }
}
