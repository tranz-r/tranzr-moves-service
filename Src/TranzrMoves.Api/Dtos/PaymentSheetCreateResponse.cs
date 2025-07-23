namespace TranzrMoves.Api.Dtos;

public class PaymentSheetCreateResponse
{
    public string PaymentIntent { get; set; }
    public string EphemeralKey { get; set; }
    public string Customer { get; set; }
    public string PublishableKey { get; set; }
}