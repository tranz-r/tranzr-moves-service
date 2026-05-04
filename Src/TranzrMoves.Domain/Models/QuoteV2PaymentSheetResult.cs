namespace TranzrMoves.Domain.Models;

public sealed class QuoteV2PaymentSheetResult
{
    public required string ClientSecret { get; init; }
    public required string IntentId { get; init; }
    public required string EphemeralKey { get; init; }
    public required string CustomerId { get; init; }
}
