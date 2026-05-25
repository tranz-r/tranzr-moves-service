using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class FuturePaymentRequest
{
    public decimal? ExtraCharges { get; set; }
    public string? ExtraChargesDescription { get; set; }
    public required string QuoteReference { get; set; }
}

public sealed class CreateQuoteV2PaymentSheetRequest
{
    public Guid QuoteId { get; set; }
    public uint ExpectedVersion { get; set; }
    public PaymentType PaymentType { get; set; }
}

public sealed class CreateQuoteV2CheckoutSessionRequest
{
    public Guid QuoteId { get; set; }
    public uint ExpectedVersion { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public sealed class CreateQuoteV2CheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string QuoteReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool EmailSent { get; set; }
}
