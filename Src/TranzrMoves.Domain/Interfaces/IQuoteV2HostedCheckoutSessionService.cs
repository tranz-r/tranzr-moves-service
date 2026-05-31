using ErrorOr;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteV2HostedCheckoutSessionService
{
    Task<ErrorOr<QuoteV2HostedCheckoutSessionResult>> CreateAsync(
        QuoteV2 quote,
        decimal amount,
        string description,
        string emailTemplateBaseName,
        PaymentType paymentMetadataType,
        string? cardErrorReason,
        IReadOnlyList<string>? bccRecipients,
        CancellationToken cancellationToken);
}
