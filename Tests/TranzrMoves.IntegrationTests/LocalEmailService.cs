using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;

using MimeKit;
using MimeKit.Text;

using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests;

public class LocalEmailService(ILogger<LocalEmailService> logger) : IEmailService
{
    public async Task SendBookingConfirmationEmailAsync(string fromEmail, string subject, string toEmail,
        string htmlEmail, string textEmail, List<string>? bccRecipients = null)
    {
        try
        {
            // create message
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = htmlEmail
            };

            if (bccRecipients != null)
            {
                foreach (var emailRecipient in bccRecipients)
                {
                    email.Bcc.Add(MailboxAddress.Parse(emailRecipient));
                }
            }

            // send email
            using var smtp = new SmtpClient();
            // Disable SSL/TLS and certificate validation for local testing
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync("localhost", 2525, SecureSocketOptions.None);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            logger.LogInformation("The email was sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("The email was not sent.");
            logger.LogError("Error message: " + ex.Message);
            logger.LogError(ex, "Failed to send order confirmation email to {Email}", toEmail);
        }
    }
}
