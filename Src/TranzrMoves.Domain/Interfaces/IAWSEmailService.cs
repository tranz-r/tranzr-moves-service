namespace TranzrMoves.Domain.Interfaces;

public interface IAwsEmailService
{
    Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, long amount, string orderId,
        DateTime orderDate);
}