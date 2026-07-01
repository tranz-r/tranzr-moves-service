using FluentValidation;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.ChangeRole;

public sealed record ChangeRoleCommand(Guid BusinessUserId, BusinessUserRole Role)
    : IRequest<ErrorOr<BusinessUserDto>>;

public sealed class ChangeRoleCommandValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(BusinessUserRole.Owner)
            .WithMessage("The Owner role cannot be assigned through the role change endpoint. Use transfer ownership.");
    }
}

public sealed class ChangeRoleCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    BusinessUserMapper mapper)
    : IRequestHandler<ChangeRoleCommand, ErrorOr<BusinessUserDto>>
{
    public async ValueTask<ErrorOr<BusinessUserDto>> Handle(
        ChangeRoleCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        // BR-007: only Owners and Admins may manage roles.
        if (caller.Role is not (BusinessUserRole.Owner or BusinessUserRole.Admin))
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have permission to change roles.");
        }

        // BR-017: a user may not modify their own role.
        if (caller.Id == command.BusinessUserId)
        {
            return Error.Validation(
                code: "BusinessUser.SelfRoleChange",
                description: "You cannot change your own role.");
        }

        var target = await businessUserRepository.GetByIdAsync(command.BusinessUserId, cancellationToken);
        if (target is null)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        // AC-016: cannot modify members of another business account.
        if (target.BusinessAccountId != caller.BusinessAccountId)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        // Role changes apply to existing memberships (Active/Suspended). Invitations carry
        // their role through the invite flow; deactivated/revoked rows are immutable here.
        if (target.Status is not (BusinessUserStatus.Active or BusinessUserStatus.Suspended))
        {
            return Error.Validation(
                code: "BusinessUser.InvalidStatusForRoleChange",
                description: "Only active or suspended members can have their role changed.");
        }

        // BR-008 / AC-004 / AC-005: Admin elevation restrictions.
        if (caller.Role == BusinessUserRole.Admin)
        {
            if (target.Role == BusinessUserRole.Owner)
            {
                return Error.Forbidden(
                    code: "BusinessUser.AdminCannotModifyOwner",
                    description: "Admins cannot modify the account owner.");
            }

            if (command.Role == BusinessUserRole.Admin)
            {
                return Error.Forbidden(
                    code: "BusinessUser.AdminCannotAssignAdmin",
                    description: "Admins can only assign Member, Finance or Viewer roles.");
            }
        }

        if (target.Role == command.Role)
        {
            // No-op: the member already has this role.
            return mapper.ToBusinessUserDto(target);
        }

        var result = await businessUserRepository.ChangeRoleAsync(
            command.BusinessUserId,
            command.Role,
            caller.Id,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        target.Role = command.Role;
        return mapper.ToBusinessUserDto(target);
    }
}
