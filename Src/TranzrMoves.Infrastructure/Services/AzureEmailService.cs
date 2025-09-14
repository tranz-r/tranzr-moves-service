using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public class AzureEmailService(ILogger<AzureEmailService> logger, EmailClient emailClient) : IEmailService
{
    public async Task SendBookingConfirmationEmailAsync(string fromEmail, string subject, string toEmail, string htmlEmail, string textEmail)
    {
        try
        {
            logger.LogInformation("Sending email using Azure communication email service...");

            var emailMessage = new EmailMessage(
                senderAddress: fromEmail, 
                content: new EmailContent(subject)
                {
                    Html = htmlEmail,
                    PlainText = textEmail
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new(toEmail)
                }));
            
            _ = await emailClient.SendAsync(
                WaitUntil.Completed,
                emailMessage);

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