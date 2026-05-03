using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public class QuoteV2CheckoutSessionIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    [Fact]
    public async Task PostSession_ReturnsNotFound_When_Quote_Does_Not_Exist()
    {
        var request = new CreateQuoteV2CheckoutSessionRequest
        {
            QuoteId = Guid.NewGuid(),
            ExpectedVersion = 0,
            Amount = 50m,
            Description = "Test"
        };

        var response =
            await Client.PostAsJsonAsync("/api/v2/checkout/session", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostSession_Returns412_When_ExpectedVersion_Does_Not_Match()
    {
        var db = fixture.DbContext!;
        db.ChangeTracker.Clear();

        var now = SystemClock.Instance.GetCurrentInstant();
        var userId = Guid.NewGuid();
        db.Set<UserV2>().Add(new UserV2
        {
            Id = userId,
            Email = "session-v2@tranzrmoves.com",
            FirstName = "S",
            LastName = "V2",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        });

        var quoteId = Guid.NewGuid();
        db.Set<QuoteV2>().Add(new QuoteV2
        {
            Id = quoteId,
            SessionId = $"sv2-{quoteId:N}"[..14],
            Type = QuoteType.Removals,
            QuoteReference = $"SV2-{quoteId:N}"[..16],
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 100m,
            CustomerId = userId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var version = await db.Set<QuoteV2>().AsNoTracking()
            .Where(q => q.Id == quoteId)
            .Select(q => q.Version)
            .SingleAsync(TestContext.Current.CancellationToken);

        var request = new CreateQuoteV2CheckoutSessionRequest
        {
            QuoteId = quoteId,
            ExpectedVersion = version + 99,
            Amount = 10m,
            Description = "x"
        };

        var response =
            await Client.PostAsJsonAsync("/api/v2/checkout/session", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    public ValueTask InitializeAsync() => new(Task.CompletedTask);

    public async ValueTask DisposeAsync() => await _resetDatabase();
}
