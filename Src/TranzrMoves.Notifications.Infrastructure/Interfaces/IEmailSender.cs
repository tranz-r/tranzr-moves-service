namespace TranzrMoves.Notifications.Infrastructure.Interfaces;

public interface IEmailSender
{
    Task<string?> SendAsync(
        string fromEmail,
        string subject,
        string toEmail,
        string htmlEmail,
        string textEmail,
        IReadOnlyList<string>? bccRecipients,
        CancellationToken cancellationToken);
}
