using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Suspend;

public sealed record SuspendBusinessUserCommand(Guid BusinessUserId) : IRequest<ErrorOr<BusinessUserDto>>;

public sealed class SuspendBusinessUserCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    BusinessUserMapper mapper)
    : IRequestHandler<SuspendBusinessUserCommand, ErrorOr<BusinessUserDto>>
{
    public async ValueTask<ErrorOr<BusinessUserDto>> Handle(
        SuspendBusinessUserCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        if (caller.Id == command.BusinessUserId)
        {
            return Error.Validation(
                code: "BusinessUser.SelfAction",
                description: "You cannot change your own membership status.");
        }

        var target = await businessUserRepository.GetByIdAsync(command.BusinessUserId, cancellationToken);
        if (target is null)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        if (target.BusinessAccountId != caller.BusinessAccountId)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business user.");
        }

        if (target.Role == BusinessUserRole.Owner)
        {
            return Error.Forbidden(
                code: "BusinessUser.OwnerProtected",
                description: "The account owner cannot be suspended.");
        }

        var result = await businessUserRepository.UpdateStatusAsync(
            command.BusinessUserId,
            BusinessUserStatus.Suspended,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        target.Status = BusinessUserStatus.Suspended;
        return mapper.ToBusinessUserDto(target);
    }
}
