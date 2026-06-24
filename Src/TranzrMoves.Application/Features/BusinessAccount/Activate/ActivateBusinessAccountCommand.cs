using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessAccount.Activate;

public sealed record ActivateBusinessAccountCommand(Guid BusinessAccountId) : IRequest<ErrorOr<BusinessAccountDto>>;

public sealed class ActivateBusinessAccountCommandHandler(
    IBusinessAccountRepository businessAccountRepository,
    BusinessAccountMapper mapper)
    : IRequestHandler<ActivateBusinessAccountCommand, ErrorOr<BusinessAccountDto>>
{
    public async ValueTask<ErrorOr<BusinessAccountDto>> Handle(
        ActivateBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await businessAccountRepository.ActivateAsync(command.BusinessAccountId, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return mapper.ToBusinessAccountDto(result.Value);
    }
}
