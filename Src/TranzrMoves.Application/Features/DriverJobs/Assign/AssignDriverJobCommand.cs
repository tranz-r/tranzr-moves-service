using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts.DriverJobs;

namespace TranzrMoves.Application.Features.DriverJobs.Assign;

public record AssignDriverJobCommand(AssignDriverJobRequest Request) : ICommand<ErrorOr<bool>>;
