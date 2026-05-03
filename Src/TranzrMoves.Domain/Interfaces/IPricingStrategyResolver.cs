using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IPricingStrategyResolver
{
    IPricingStrategy Resolve(QuoteType quoteType);
}
