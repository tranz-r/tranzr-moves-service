using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using TranzrMoves.Notifications.Application.Handlers;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Application.Telemetry;
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

        var handler = CreateHandler(db, out var emailSender);

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
    public async Task Handle_Marketing_WithoutConsent_SkipsWithoutSending()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db, out var emailSender);

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
        row.Error.Should().Be("Marketing consent not granted");
    }

    [Fact]
    public async Task Handle_Marketing_WithEmailConsent_SendsEmail()
    {
        await using var db = CreateDbContext();
        var now = SystemClock.Instance.GetCurrentInstant();
        db.CustomerMarketingPreferences.Add(new CustomerMarketingPreference
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            EmailMarketingEnabled = true,
            SmsMarketingEnabled = false,
            EmailMarketingConsentedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out var emailSender);
        emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>?>(),
                Arg.Any<CancellationToken>())
            .Returns("provider-id");

        var message = new SendNotification(
            Guid.NewGuid(),
            "quote-1",
            NotificationCategory.Marketing,
            NotificationChannel.Email,
            "quote-reminder",
            null,
            "test@example.com",
            null,
            "Finish your quote",
            new Dictionary<string, object?>());

        await handler.Handle(message, CancellationToken.None);

        await emailSender.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            "test@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>());

        var row = await db.NotificationDeliveries.SingleAsync();
        row.Status.Should().Be(NotificationDeliveryStatus.Succeeded);
    }

    [Fact]
    public async Task Handle_Marketing_EmailOptedOut_SkipsWithoutSending()
    {
        await using var db = CreateDbContext();
        var now = SystemClock.Instance.GetCurrentInstant();
        db.CustomerMarketingPreferences.Add(new CustomerMarketingPreference
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            EmailMarketingEnabled = false,
            SmsMarketingEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out var emailSender);

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
    }

    private static SendNotificationHandler CreateHandler(
        NotificationsDbContext db,
        out IEmailSender emailSender)
    {
        var templateService = Substitute.For<ITemplateService>();
        templateService.GenerateEmail(Arg.Any<string>(), Arg.Any<object>())
            .Returns("<html></html>", "text");
        emailSender = Substitute.For<IEmailSender>();
        var marketingPreferenceService = new MarketingPreferenceService(
            new Infrastructure.Repositories.MarketingPreferenceRepository(db),
            SystemClock.Instance);

        return new SendNotificationHandler(
            db,
            templateService,
            emailSender,
            marketingPreferenceService,
            new NotificationsMetrics(),
            SystemClock.Instance,
            NullLogger<SendNotificationHandler>.Instance);
    }

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsDbContext(options);
    }
}
