using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts.DriverJobs;

namespace TranzrMoves.Application.Features.DriverJobs.Unassign;

public record UnassignDriverJobCommand(UnassignDriverJobRequest Request) : ICommand<ErrorOr<bool>>;
