using System.Collections.Immutable;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.DriverJobs.List;

public record ListDriverJobsQuery(Guid DriverId, IEnumerable<PaymentStatus>? Statuses) : IQuery<ImmutableList<JobDto>>;
