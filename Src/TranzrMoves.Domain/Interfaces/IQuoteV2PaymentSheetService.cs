using ErrorOr;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteV2PaymentSheetService
{
    Task<ErrorOr<QuoteV2PaymentSheetResult>> CreateAsync(
        QuoteV2 quote,
        PaymentType paymentType,
        CancellationToken ct);
}
