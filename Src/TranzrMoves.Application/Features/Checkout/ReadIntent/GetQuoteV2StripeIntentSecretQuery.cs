using Mediator;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed record GetQuoteV2StripeIntentSecretQuery(Guid QuoteId) : IQuery<ErrorOr<StripeIntentClientSecret>>;
