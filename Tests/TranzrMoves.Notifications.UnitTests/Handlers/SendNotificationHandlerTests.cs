using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using TranzrMoves.Notifications.Application.Handlers;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure;
using TranzrMoves.Notifications.Infrastructure.Entities;
using TranzrMoves.Notifications.Infrastructure.Interfaces;

namespace TranzrMoves.Notifications.UnitTests.Handlers;

public sealed class SendNotificationHandlerTests
{
    [Fact]
    public async Task Handle_WhenAlreadySucceeded_SkipsEmailSend()
    {
        var messageId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.NotificationDeliveries.Add(new NotificationDelivery
        {
            MessageId = messageId,
            CorrelationId = "corr-1",
            TemplateKey = "deposit-confirmation",
            ToEmail = "test@example.com",
            Category = NotificationCategory.Transactional.ToString(),
            Status = NotificationDeliveryStatus.Succeeded,
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        });
        await db.SaveChangesAsync();

        var templateService = Substitute.For<ITemplateService>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new SendNotificationHandler(
            db,
            templateService,
            emailSender,
            SystemClock.Instance,
            NullLogger<SendNotificationHandler>.Instance);

        var message = new SendNotification(
            messageId,
            "corr-1",
            NotificationCategory.Transactional,
            NotificationChannel.Email,
            "deposit-confirmation",
            null,
            "test@example.com",
            null,
            "Subject",
            new Dictionary<string, object?>());

        await handler.Handle(message, CancellationToken.None);

        await emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            default!, default!, default!, default!, default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Marketing_SkipsWithoutSending()
    {
        await using var db = CreateDbContext();
        var templateService = Substitute.For<ITemplateService>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new SendNotificationHandler(
            db,
            templateService,
            emailSender,
            SystemClock.Instance,
            NullLogger<SendNotificationHandler>.Instance);

        var message = new SendNotification(
            Guid.NewGuid(),
            "quote-1",
            NotificationCategory.Marketing,
            NotificationChannel.Email,
            "quote-reminder",
            null,
            "test@example.com",
            null,
            null,
            new Dictionary<string, object?>());

        await handler.Handle(message, CancellationToken.None);

        await emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            default!, default!, default!, default!, default!, default!, default!);

        var row = await db.NotificationDeliveries.SingleAsync();
        row.Status.Should().Be(NotificationDeliveryStatus.Skipped);
    }

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsDbContext(options);
    }
}
