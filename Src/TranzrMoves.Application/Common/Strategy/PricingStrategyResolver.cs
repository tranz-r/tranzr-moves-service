using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Common.Strategy;

public sealed class PricingStrategyResolver(IEnumerable<IPricingStrategy> strategies) : IPricingStrategyResolver
{
    public IPricingStrategy Resolve(QuoteType quoteType)
    {
        var strategy = strategies.SingleOrDefault(x => x.CanHandle(quoteType));

        return strategy ?? throw new InvalidOperationException($"No pricing strategy is registered for quote type '{quoteType}'.");
    }
}
