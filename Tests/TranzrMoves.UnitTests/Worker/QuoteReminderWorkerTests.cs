using FluentAssertions;
using NodaTime;
using TranzrMoves.Worker.HostedServices;

namespace TranzrMoves.UnitTests.Worker;

public sealed class QuoteReminderWorkerTests
{
    [Fact]
    public void CreateReminderMessageId_IsStableWithinCooldownWindow()
    {
        var quoteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var now = Instant.FromUtc(2026, 6, 7, 12, 0);

        var first = QuoteReminderWorker.CreateReminderMessageId(quoteId, now, 7);
        var second = QuoteReminderWorker.CreateReminderMessageId(quoteId, now.Plus(Duration.FromHours(2)), 7);

        first.Should().Be(second);
    }

    [Fact]
    public void BuildResumeUrl_UsesQuoteResumeEntryPath()
    {
        var url = QuoteReminderWorker.BuildResumeUrl(
            "http://localhost:3000/",
            QuoteReminderWorker.QuoteResumeFrontendPath,
            "token/with+special=chars");

        url.Should().Be("http://localhost:3000/moves/quote?token=token%2Fwith%2Bspecial%3Dchars");
    }

    [Fact]
    public void CreateReminderMessageId_ChangesAcrossCooldownWindows()
    {
        var quoteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var firstWindow = Instant.FromUtc(2026, 6, 7, 12, 0);
        var nextWindow = Instant.FromUtc(2026, 6, 15, 12, 0);

        var first = QuoteReminderWorker.CreateReminderMessageId(quoteId, firstWindow, 7);
        var second = QuoteReminderWorker.CreateReminderMessageId(quoteId, nextWindow, 7);

        first.Should().NotBe(second);
    }
}
