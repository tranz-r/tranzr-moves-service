using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.List;

public sealed record ListBusinessUsersQuery : IRequest<ErrorOr<IReadOnlyList<BusinessUserDto>>>;

public sealed class ListBusinessUsersQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    BusinessUserMapper mapper)
    : IRequestHandler<ListBusinessUsersQuery, ErrorOr<IReadOnlyList<BusinessUserDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<BusinessUserDto>>> Handle(
        ListBusinessUsersQuery query,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        var users = await businessUserRepository.GetByBusinessAccountIdAsync(
            caller.BusinessAccountId,
            cancellationToken);

        return ErrorOrFactory.From(mapper.ToBusinessUserDtos(users));
    }
}
