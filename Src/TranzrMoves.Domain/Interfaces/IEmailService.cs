namespace TranzrMoves.Domain.Interfaces;

public interface IEmailService
{
    Task SendBookingConfirmationEmailAsync(string fromEMail, string subject, string toEmail, string htmlEmail, string textEmail);
}