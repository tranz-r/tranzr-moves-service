using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts.DriverJobs;

namespace TranzrMoves.Application.Features.DriverJobs.Assign;

public record AssignDriverQuoteCommand(AssignDriverQuoteRequest Request) : ICommand<ErrorOr<bool>>;
