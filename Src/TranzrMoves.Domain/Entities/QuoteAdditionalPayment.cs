namespace TranzrMoves.Domain.Entities;

public class QuoteAdditionalPayment
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ReceiptUrl { get; set; }
}