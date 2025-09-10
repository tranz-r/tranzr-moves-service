namespace TranzrMoves.Domain.Interfaces;

public interface IAwsEmailService
{
    Task SendBookingConfirmationEmailAsync(string subject, string toEmail, string htmlEmail, string textEmail);
}