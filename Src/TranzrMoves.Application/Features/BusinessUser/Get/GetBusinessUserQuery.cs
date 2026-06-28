using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Get;

public sealed record GetBusinessUserQuery(Guid Id) : IRequest<ErrorOr<BusinessUserDto>>;

public sealed class GetBusinessUserQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    BusinessUserMapper mapper)
    : IRequestHandler<GetBusinessUserQuery, ErrorOr<BusinessUserDto>>
{
    public async ValueTask<ErrorOr<BusinessUserDto>> Handle(
        GetBusinessUserQuery query,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        var target = await businessUserRepository.GetByIdAsync(query.Id, cancellationToken);
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

        return mapper.ToBusinessUserDto(target);
    }
}
