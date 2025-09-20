using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Api.Dtos;

public class CreatePaymentLinkRequest
{
    public Guid QuoteId { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal? Amount { get; set; } // Optional - if not provided, will use quote total or deposit amount
    public string? Description { get; set; } // Optional - if not provided, will generate based on payment type
}

public class CreatePaymentLinkResponse
{
    public string PaymentLinkId { get; set; } = string.Empty;
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string QuoteReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public bool EmailSent { get; set; }
}

