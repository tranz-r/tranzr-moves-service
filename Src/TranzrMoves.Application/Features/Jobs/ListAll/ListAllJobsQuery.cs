using System.Collections.Immutable;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Jobs.ListAll;

public sealed record ListAllJobsQuery() : IQuery<ImmutableList<JobDto>>;
