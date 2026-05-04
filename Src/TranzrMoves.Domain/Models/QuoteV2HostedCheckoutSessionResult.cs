namespace TranzrMoves.Domain.Models;

public sealed record QuoteV2HostedCheckoutSessionResult(
    string SessionId,
    string Url,
    string QuoteReference,
    decimal Amount,
    bool EmailSent);
