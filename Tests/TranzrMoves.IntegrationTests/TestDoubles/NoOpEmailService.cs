using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

internal sealed class NoOpEmailService : IEmailService
{
    public Task SendBookingConfirmationEmailAsync(string fromEmail, string subject, string toEmail, string htmlEmail,
        string textEmail, List<string>? bccRecipients = null) =>
        Task.CompletedTask;
}
