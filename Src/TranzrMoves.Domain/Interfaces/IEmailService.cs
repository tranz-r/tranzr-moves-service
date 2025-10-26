namespace TranzrMoves.Domain.Interfaces;

public interface IEmailService
{
    Task SendBookingConfirmationEmailAsync(string fromEmail, string subject, string toEmail, string htmlEmail,
        string textEmail, List<string>? bccRecipients = null);
}
