using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Invite;

public sealed record InviteBusinessUserCommand(
    string FirstName,
    string LastName,
    string Email,
    BusinessUserRole Role) : IRequest<ErrorOr<InviteBusinessUserResponse>>;

public sealed class InviteBusinessUserCommandValidator : AbstractValidator<InviteBusinessUserCommand>
{
    public InviteBusinessUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
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
    ILogger<InviteBusinessUserCommandHandler> logger)
    : IRequestHandler<InviteBusinessUserCommand, ErrorOr<InviteBusinessUserResponse>>
{
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
        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();

        var existingUser = await userV2Repository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            // BR-001: a user can belong to only one Business Account (cross-tenant check).
            var existingMembership = await businessUserRepository.GetByUserIdGlobalAsync(
                existingUser.Id,
                cancellationToken);
            if (existingMembership is not null)
            {
                return Error.Conflict(
                    code: "BusinessUser.AlreadyMember",
                    description: "This user already belongs to a business account.");
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
        };
    }
}
