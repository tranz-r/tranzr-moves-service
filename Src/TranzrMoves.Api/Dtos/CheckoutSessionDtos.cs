namespace TranzrMoves.Api.Dtos;

public class GetCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}


