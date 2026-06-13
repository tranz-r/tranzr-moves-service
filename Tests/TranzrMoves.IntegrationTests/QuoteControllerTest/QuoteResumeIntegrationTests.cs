using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Services;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;

namespace TranzrMoves.IntegrationTests.QuoteControllerTest;

public sealed class QuoteResumeIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private const string GuestCookieName = "tranzr_guest";

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Resume_ReturnsUnauthorized_When_GuestCookieMissing()
    {
        var quote = await SeedQuoteAsync("original-session");

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var response = await PostResumeAsync(token, guestId: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resume_ReturnsOk_WithoutRebind_When_GuestCookieMatchesQuoteSession()
    {
        const string sessionId = "same-device-session";
        var quote = await SeedQuoteAsync(sessionId);

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var response = await PostResumeAsync(token, sessionId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var journey = await response.Content.ReadFromJsonAsync<QuoteJourneyState>(
            cancellationToken: TestContext.Current.CancellationToken);
        journey.Should().NotBeNull();
        journey!.IsResumable.Should().BeTrue();
        journey.QuoteId.Should().Be(quote.Id);

        var persistedSession = await GetPersistedSessionIdAsync(quote.Id);
        persistedSession.Should().Be(sessionId);
    }

    [Fact]
    public async Task Resume_RebindsSession_When_GuestCookieDiffersFromQuoteSession()
    {
        const string originalSession = "device-a-session";
        const string newSession = "device-b-session";
        var quote = await SeedQuoteAsync(originalSession);

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var response = await PostResumeAsync(token, newSession);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var persistedSession = await GetPersistedSessionIdAsync(quote.Id);
        persistedSession.Should().Be(newSession);
    }

    [Fact]
    public async Task Resume_ReturnsUnauthorized_When_TokenSessionStaleAfterRebind()
    {
        const string originalSession = "device-a-session";
        const string newSession = "device-b-session";
        var quote = await SeedQuoteAsync(originalSession);

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var firstResponse = await PostResumeAsync(token, newSession);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staleResponse = await PostResumeAsync(token, originalSession);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resume_AllowsInitToLoadSameQuote_AfterCrossDeviceRebind()
    {
        const string originalSession = "device-a-session";
        const string newSession = "device-b-session";
        var quote = await SeedQuoteAsync(
            originalSession,
            lastCompletedStepKey: QuoteStepKeys.CollectionDeliveryAddresses);

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var resumeResponse = await PostResumeAsync(token, newSession);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initResponse = await PostInitAsync(newSession, QuoteType.Send);
        initResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var initJson = await JsonDocument.ParseAsync(
            await initResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var initQuoteId = initJson.RootElement.GetProperty("quote").GetProperty("id").GetGuid();
        initQuoteId.Should().Be(quote.Id);
    }

    [Fact]
    public async Task Resume_ReturnsBadRequest_When_QuoteExpired()
    {
        const string sessionId = "expired-quote-session";
        var quote = await SeedQuoteAsync(
            sessionId,
            expiresAt: SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(1)));

        await using var scope = fixture.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var token = tokenService.Create(quote, TimeSpan.FromDays(30));

        var response = await PostResumeAsync(token, sessionId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var journey = await response.Content.ReadFromJsonAsync<QuoteJourneyState>(
            cancellationToken: TestContext.Current.CancellationToken);
        journey.Should().NotBeNull();
        journey!.IsResumable.Should().BeFalse();
        journey.ReasonIfNotResumable.Should().Be("Quote has expired.");
    }

    [Fact]
    public async Task Resume_ReturnsBadRequest_When_TokenInvalid()
    {
        var response = await PostResumeAsync("not-a-valid-token", "any-session");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private async Task<QuoteV2> SeedQuoteAsync(
        string sessionId,
        string? lastCompletedStepKey = null,
        Instant? expiresAt = null)
    {
        var db = fixture.DbContext!;
        db.ChangeTracker.Clear();

        var now = SystemClock.Instance.GetCurrentInstant();
        var quoteId = Guid.NewGuid();
        var quote = new QuoteV2
        {
            Id = quoteId,
            SessionId = sessionId,
            Type = QuoteType.Send,
            QuoteReference = $"RES-{quoteId:N}"[..16],
            VanType = VanType.largeVan,
            CrewCount = 1,
            LastCompletedStepKey = lastCompletedStepKey,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        db.Set<QuoteV2>().Add(quote);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        return quote;
    }

    private async Task<string> GetPersistedSessionIdAsync(Guid quoteId)
    {
        var db = fixture.DbContext!;
        db.ChangeTracker.Clear();

        return await db.Set<QuoteV2>()
            .AsNoTracking()
            .Where(q => q.Id == quoteId)
            .Select(q => q.SessionId)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostResumeAsync(string token, string? guestId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/quote/resume")
        {
            Content = JsonContent.Create(new ResumeQuoteRequest { Token = token })
        };

        if (guestId is not null)
        {
            request.Headers.Add("Cookie", $"{GuestCookieName}={guestId}");
        }

        return fixture.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostInitAsync(string guestId, QuoteType quoteType)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/quote/init")
        {
            Content = JsonContent.Create(new { quoteType })
        };
        request.Headers.Add("Cookie", $"{GuestCookieName}={guestId}");

        return fixture.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
    }
}
