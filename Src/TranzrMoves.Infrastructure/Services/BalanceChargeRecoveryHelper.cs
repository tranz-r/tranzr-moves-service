using NodaTime.Text;
using Stripe;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Services;

internal static class BalanceChargeRecoveryHelper
{
    private static readonly LocalDatePattern IsoDatePattern = LocalDatePattern.Iso;

    public static string BuildCardErrorDescription(QuoteV2 quote, string cardBrand, string cardLast4, string cardErrorReason)
    {
        var collectionDate = quote.Schedule?.CollectionDate?.InUtc().Date;
        var collectionDateText = collectionDate is { } d ? IsoDatePattern.Format(d) : string.Empty;

        return
            $"<b>We couldn't charge your {cardBrand} card ending with {cardLast4}, because {cardErrorReason}.</b> " +
            $"<p>Please use the secure link below to complete the payment for your quotation {quote.QuoteReference} with Tranzr Moves.</p> " +
            "To avoid any delay or cancellation of your scheduled service on " +
            $"{collectionDateText}, kindly make your payment as soon as possible or contact us to arrange an alternative payment option.";
    }

    public static string MapCardErrorReason(StripeException ex) =>
        ex.StripeError.Code switch
        {
            "card_declined" when ex.StripeError.DeclineCode == "insufficient_funds" =>
                "the card was declined due to insufficient funds",
            "card_declined" => "the card was declined.",
            "expired_card" => "the card has expired.",
            "incorrect_cvc" => "the card's security code is incorrect.",
            "incorrect_number" => "the card number is incorrect.",
            "invalid_cvc" => "the card's security code is invalid.",
            "invalid_expiry_month" => "the expiry month is invalid.",
            "invalid_expiry_year" => "the expiry year is invalid.",
            "processing_error" => "an error occurred while processing the card.",
            "incorrect_zip" => "the card's post code failed validation.",
            "authentication_required" => "the card's authorization is required",
            "approve_with_id" => "the payment cannot be authorized",
            "call_issuer" => "the card has been declined, call issuer",
            "do_not_honor" => "the card has been declined",
            "insufficient_funds" => "the card has insufficient funds.",
            "invalid_account" => "the card or account is invalid.",
            "currency_not_supported" => "the card does not support this currency.",
            "lost_card" => "the card has been reported lost.",
            "stolen_card" => "the card has been reported stolen.",
            _ => ex.StripeError.Message ?? "the payment failed."
        };
}
