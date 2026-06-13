using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NSubstitute;
using TranzrMoves.Notifications.Application.Handlers;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Application.Telemetry;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure;
using TranzrMoves.Notifications.Infrastructure.Interfaces;

namespace TranzrMoves.Notifications.IntegrationTests;

public sealed class MarketingNotificationFlowTests
{
    [Fact]
    public async Task PreferenceGrantThenMarketingSend_Succeeds()
    {
        await using var db = CreateDbContext();
        var preferenceService = new MarketingPreferenceService(
            new Infrastructure.Repositories.MarketingPreferenceRepository(db),
            SystemClock.Instance);

        await preferenceService.ApplyPreferencesAsync(
            new ApplyMarketingPreferencesRequest(
                "flow@example.com",
                EmailMarketingEnabled: true,
                SmsMarketingEnabled: false,
                MarketingConsentSource.QuoteJourney),
            CancellationToken.None);

        var templateService = Substitute.For<ITemplateService>();
        templateService.GenerateEmail(Arg.Any<string>(), Arg.Any<object>())
            .Returns("<html></html>", "text");
        var emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>?>(),
                Arg.Any<CancellationToken>())
            .Returns("provider-id");

        var sendHandler = new SendNotificationHandler(
            db,
            templateService,
            emailSender,
            preferenceService,
            new NotificationsMetrics(),
            SystemClock.Instance,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SendNotificationHandler>.Instance);

        await sendHandler.Handle(
            new SendNotification(
                Guid.NewGuid(),
                "quote-1",
                NotificationCategory.Marketing,
                NotificationChannel.Email,
                "quote-reminder",
                null,
                "flow@example.com",
                null,
                "Finish your quote",
                new Dictionary<string, object?>()),
            CancellationToken.None);

        var delivery = await db.NotificationDeliveries.SingleAsync();
        delivery.Status.Should().Be(Infrastructure.Entities.NotificationDeliveryStatus.Succeeded);
    }

    [Fact]
    public async Task MarketingSendWithoutConsent_IsSkipped()
    {
        await using var db = CreateDbContext();
        var templateService = Substitute.For<ITemplateService>();
        var emailSender = Substitute.For<IEmailSender>();
        var preferenceService = new MarketingPreferenceService(
            new Infrastructure.Repositories.MarketingPreferenceRepository(db),
            SystemClock.Instance);

        var sendHandler = new SendNotificationHandler(
            db,
            templateService,
            emailSender,
            preferenceService,
            new NotificationsMetrics(),
            SystemClock.Instance,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SendNotificationHandler>.Instance);

        await sendHandler.Handle(
            new SendNotification(
                Guid.NewGuid(),
                "quote-1",
                NotificationCategory.Marketing,
                NotificationChannel.Email,
                "quote-reminder",
                null,
                "missing-consent@example.com",
                null,
                null,
                new Dictionary<string, object?>()),
            CancellationToken.None);

        var delivery = await db.NotificationDeliveries.SingleAsync();
        delivery.Status.Should().Be(Infrastructure.Entities.NotificationDeliveryStatus.Skipped);
        await emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            default!, default!, default!, default!, default!, default!, default!);
    }

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsDbContext(options);
    }
}
