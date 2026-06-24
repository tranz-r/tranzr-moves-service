using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessAccount.Get;

public sealed record GetBusinessAccountQuery(Guid Id) : IRequest<ErrorOr<BusinessAccountDto>>;

public sealed class GetBusinessAccountQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessAccountRepository businessAccountRepository,
    BusinessAccountMapper mapper)
    : IRequestHandler<GetBusinessAccountQuery, ErrorOr<BusinessAccountDto>>
{
    public async ValueTask<ErrorOr<BusinessAccountDto>> Handle(
        GetBusinessAccountQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null)
        {
            return Error.Forbidden(
                code: "BusinessAccount.Forbidden",
                description: "You do not have access to this business account.");
        }

        if (businessUser.BusinessAccountId != query.Id)
        {
            return Error.Forbidden(
                code: "BusinessAccount.Forbidden",
                description: "You do not have access to this business account.");
        }

        var account = await businessAccountRepository.GetByIdAsync(query.Id, cancellationToken);
        if (account is null)
        {
            return Error.NotFound(
                code: "BusinessAccount.NotFound",
                description: "Business account not found.");
        }

        return mapper.ToBusinessAccountDto(account);
    }
}
