using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure;

namespace TranzrMoves.Notifications.UnitTests.Services;

public sealed class MarketingPreferenceServiceTests
{
    [Fact]
    public async Task ApplyPreferencesAsync_CreatesPreferenceAndGrantedEvents()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.ApplyPreferencesAsync(
            new ApplyMarketingPreferencesRequest(
                "User@Example.com",
                EmailMarketingEnabled: true,
                SmsMarketingEnabled: true,
                MarketingConsentSource.QuoteJourney,
                CustomerId: Guid.NewGuid()),
            CancellationToken.None);

        result.Email.Should().Be("user@example.com");
        result.EmailMarketingEnabled.Should().BeTrue();
        result.SmsMarketingEnabled.Should().BeTrue();

        var preference = await db.CustomerMarketingPreferences.SingleAsync();
        preference.Email.Should().Be("user@example.com");
        preference.EmailMarketingConsentedAt.Should().NotBeNull();
        preference.SmsMarketingConsentedAt.Should().NotBeNull();

        var events = await db.MarketingConsentEvents.ToListAsync();
        events.Should().HaveCount(2);
        events.Should().OnlyContain(x => x.EventType == MarketingConsentEventType.Granted);
    }

    [Fact]
    public async Task ApplyPreferencesAsync_IsIdempotentWhenFlagsUnchanged()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var request = new ApplyMarketingPreferencesRequest(
            "user@example.com",
            EmailMarketingEnabled: true,
            SmsMarketingEnabled: false,
            MarketingConsentSource.QuoteJourney);

        await service.ApplyPreferencesAsync(request, CancellationToken.None);
        await service.ApplyPreferencesAsync(request, CancellationToken.None);

        (await db.MarketingConsentEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApplyPreferencesAsync_WithdrawnEventWhenOptingOut()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.ApplyPreferencesAsync(
            new ApplyMarketingPreferencesRequest(
                "user@example.com",
                EmailMarketingEnabled: true,
                SmsMarketingEnabled: false,
                MarketingConsentSource.QuoteJourney),
            CancellationToken.None);

        await service.ApplyPreferencesAsync(
            new ApplyMarketingPreferencesRequest(
                "user@example.com",
                EmailMarketingEnabled: false,
                SmsMarketingEnabled: false,
                MarketingConsentSource.PreferenceCentre),
            CancellationToken.None);

        var preference = await db.CustomerMarketingPreferences.SingleAsync();
        preference.EmailMarketingEnabled.Should().BeFalse();
        preference.EmailMarketingConsentedAt.Should().NotBeNull();

        var events = await db.MarketingConsentEvents.OrderBy(x => x.OccurredAt).ToListAsync();
        events.Should().HaveCount(2);
        events[0].EventType.Should().Be(MarketingConsentEventType.Granted);
        events[1].EventType.Should().Be(MarketingConsentEventType.Withdrawn);
    }

    [Fact]
    public async Task IsChannelEnabledAsync_DefaultDenyWhenMissing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        (await service.IsChannelEnabledAsync("missing@example.com", MarketingConsentChannel.Email, CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreatePreferenceIdAsync_ReturnsExistingId()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var customerId = Guid.NewGuid();

        var first = await service.GetOrCreatePreferenceIdAsync("user@example.com", customerId, CancellationToken.None);
        var second = await service.GetOrCreatePreferenceIdAsync("user@example.com", customerId, CancellationToken.None);

        first.Should().Be(second);
        (await db.CustomerMarketingPreferences.CountAsync()).Should().Be(1);
    }

    private static MarketingPreferenceService CreateService(NotificationsDbContext db) =>
        new(new Infrastructure.Repositories.MarketingPreferenceRepository(db), SystemClock.Instance);

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsDbContext(options);
    }
}
