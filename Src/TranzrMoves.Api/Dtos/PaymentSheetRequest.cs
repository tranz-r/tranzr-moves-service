namespace TranzrMoves.Api.Dtos;

public class PaymentSheetRequest
{
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required long Amount { get; set; }
}