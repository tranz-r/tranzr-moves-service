using ErrorOr;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteV2DepositBalancePaymentService
{
    Task<ErrorOr<StripeIntentClientSecret>> CreateDepositBalanceAsync(
        QuoteV2 quote,
        decimal? extraCharges,
        string? extraChargesDescription,
        CancellationToken ct);
}
