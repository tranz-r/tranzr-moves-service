using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Jobs.GetByQuoteId;

public sealed record GetJobByQuoteIdQuery(string QuoteId) : IQuery<ErrorOr<JobDto>>;
