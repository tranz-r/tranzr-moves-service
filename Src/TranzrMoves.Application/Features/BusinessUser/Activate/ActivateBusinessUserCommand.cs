using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Activate;

public sealed record ActivateBusinessUserCommand(Guid BusinessUserId) : IRequest<ErrorOr<BusinessUserDto>>;

public sealed class ActivateBusinessUserCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    BusinessUserMapper mapper)
    : IRequestHandler<ActivateBusinessUserCommand, ErrorOr<BusinessUserDto>>
{
    public async ValueTask<ErrorOr<BusinessUserDto>> Handle(
        ActivateBusinessUserCommand command,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
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

        // BR-009: deactivation is permanent and cannot be reversed via activation.
        if (target.Status == BusinessUserStatus.Deactivated)
        {
            return Error.Validation(
                code: "BusinessUser.Deactivated",
                description: "A deactivated business user cannot be reactivated.");
        }

        var result = await businessUserRepository.UpdateStatusAsync(
            command.BusinessUserId,
            BusinessUserStatus.Active,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        target.Status = BusinessUserStatus.Active;
        return mapper.ToBusinessUserDto(target);
    }
}
