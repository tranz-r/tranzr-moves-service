using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Checkout.CreateQuoteV2CheckoutSession;

public sealed class CreateQuoteV2CheckoutSessionCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteV2HostedCheckoutSessionService hostedCheckoutSessionService,
    ILogger<CreateQuoteV2CheckoutSessionCommandHandler> logger)
    : ICommandHandler<CreateQuoteV2CheckoutSessionCommand, ErrorOr<CreateQuoteV2CheckoutSessionResponse>>
{
    public async ValueTask<ErrorOr<CreateQuoteV2CheckoutSessionResponse>> Handle(
        CreateQuoteV2CheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);
        if (quote is null)
        {
            return Error.NotFound("QuoteV2.NotFound", "QuoteV2 not found.");
        }

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        if (quote.Customer?.Email is null or "")
        {
            return Error.Validation("QuoteV2.Customer.Email", "QuoteV2 customer email is required.");
        }

        if (command.Amount <= 0)
        {
            return Error.Validation("Amount", "Amount must be positive.");
        }

        var description = command.Description ?? string.Empty;
        var checkout = await hostedCheckoutSessionService.CreateAsync(
            quote,
            command.Amount,
            description,
            "checkout-session",
            PaymentType.Adhoc,
            cardErrorReason: null,
            bccRecipients: null,
            cancellationToken);

        if (checkout.IsError)
        {
            logger.LogWarning("Hosted checkout failed for QuoteV2 {QuoteId}: {Errors}", command.QuoteId,
                checkout.Errors);
            return checkout.Errors;
        }

        var r = checkout.Value;
        return new CreateQuoteV2CheckoutSessionResponse
        {
            SessionId = r.SessionId,
            Url = r.Url,
            QuoteReference = r.QuoteReference,
            Amount = r.Amount,
            EmailSent = r.EmailSent
        };
    }
}
