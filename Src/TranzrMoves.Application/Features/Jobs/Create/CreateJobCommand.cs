using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Jobs.Create;

public record CreateJobCommand(JobDto JobDto) : ICommand<ErrorOr<JobDto>>;