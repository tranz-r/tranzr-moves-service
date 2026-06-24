using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessAccount.Suspend;

public sealed record SuspendBusinessAccountCommand(Guid BusinessAccountId) : IRequest<ErrorOr<BusinessAccountDto>>;

public sealed class SuspendBusinessAccountCommandHandler(
    IBusinessAccountRepository businessAccountRepository,
    BusinessAccountMapper mapper)
    : IRequestHandler<SuspendBusinessAccountCommand, ErrorOr<BusinessAccountDto>>
{
    public async ValueTask<ErrorOr<BusinessAccountDto>> Handle(
        SuspendBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await businessAccountRepository.SuspendAsync(command.BusinessAccountId, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return mapper.ToBusinessAccountDto(result.Value);
    }
}
