namespace TranzrMoves.Api.Services;

public interface IEmailService
{
    Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, long amount, string orderId, DateTime orderDate);
} 