using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using TranzrMoves.Notifications.Infrastructure.Interfaces;

namespace TranzrMoves.Notifications.Infrastructure.Services;

public sealed class AzureEmailSender(ILogger<AzureEmailSender> logger, EmailClient emailClient) : IEmailSender
{
    public async Task<string?> SendAsync(
        string fromEmail,
        string subject,
        string toEmail,
        string htmlEmail,
        string textEmail,
        IReadOnlyList<string>? bccRecipients,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var toEmailAddresses = new List<EmailAddress> { new(toEmail) };
        List<EmailAddress>? bccRecipientList = [];
        if (bccRecipients is not null)
        {
            foreach (var bcc in bccRecipients)
            {
                bccRecipientList.Add(new EmailAddress(bcc));
            }
        }

        var emailMessage = new EmailMessage(
            senderAddress: fromEmail,
            content: new EmailContent(subject)
            {
                Html = htmlEmail,
                PlainText = textEmail
            },
            recipients: new EmailRecipients(to: toEmailAddresses, bcc: bccRecipientList));

        var operation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
        logger.LogInformation("Email sent to {Email}", toEmail);
        return operation.Id;
    }
}
