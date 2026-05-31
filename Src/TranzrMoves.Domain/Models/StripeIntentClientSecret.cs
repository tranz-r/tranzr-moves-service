namespace TranzrMoves.Domain.Models;

public sealed class StripeIntentClientSecret
{
    public required string ClientSecret { get; init; }
    public required string IntentId { get; init; }
}
