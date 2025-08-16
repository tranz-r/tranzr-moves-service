using System.Collections.Immutable;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.CustomerJobs.List;

public record ListCustomerJobsQuery(Guid CustomerId, IEnumerable<PaymentStatus>? Statuses) : IQuery<ImmutableList<JobDto>>;
