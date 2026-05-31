namespace TranzrMoves.Domain.Models;

public sealed class CheckoutStripeSessionSummary
{
    public required string SessionId { get; init; }
    public string? CustomerId { get; init; }
    public string? PaymentIntentId { get; init; }
    public required string Status { get; init; }
    public required string PaymentStatus { get; init; }
    public required string Url { get; init; }
}
