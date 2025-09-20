namespace TranzrMoves.Api.Dtos;

public class CreateCheckoutSessionRequest
{
    public string QuoteReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string QuoteReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool EmailSent { get; set; }
}

public class GetCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}


