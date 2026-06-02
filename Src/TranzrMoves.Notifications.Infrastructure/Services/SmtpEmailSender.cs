using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TranzrMoves.Notifications.Infrastructure.Interfaces;
using TranzrMoves.Notifications.Infrastructure.Options;

namespace TranzrMoves.Notifications.Infrastructure.Services;

public sealed class SmtpEmailSender(
    ILogger<SmtpEmailSender> logger,
    IOptions<NotificationsOptions> notificationsOptions) : IEmailSender
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

        var smtp = notificationsOptions.Value.Smtp;
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        if (bccRecipients is not null)
        {
            foreach (var bcc in bccRecipients)
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlEmail,
            TextBody = textEmail
        }.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = smtp.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(smtp.Host, smtp.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            if (string.IsNullOrWhiteSpace(smtp.Password))
            {
                throw new InvalidOperationException(
                    "Notifications:Smtp:Password is required when Notifications:Smtp:Username is set.");
            }

            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        var providerId = Guid.NewGuid().ToString("N");
        logger.LogInformation("SMTP email sent to {Email} via {Host}:{Port}", toEmail, smtp.Host, smtp.Port);
        return providerId;
    }
}
