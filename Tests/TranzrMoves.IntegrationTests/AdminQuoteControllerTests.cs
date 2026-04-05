using System.Net;
using System.Text.Json;
using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;

using TranzrMoves.Application.Features.Admin.Quote.List;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests;

public class AdminQuoteControllerTests(TestServerFixture fixture) : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions AdminQuoteJsonOptions = CreateAdminQuoteJsonOptions();

    private static JsonSerializerOptions CreateAdminQuoteJsonOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return o;
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    [Fact]
    public async Task GetAdminQuotes_WithValidParameters_ShouldReturnPaginatedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?admin=true&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Pagination.Should().NotBeNull();
        result.Pagination.Page.Should().Be(1);
        result.Pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAdminQuotes_WithSearch_ShouldFilterResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?admin=true&search=TEST-REF-001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.First().QuoteReference.Should().Contain("TEST-REF-001");
    }

    [Fact]
    public async Task GetAdminQuotes_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?admin=true&sortBy=createdAt&sortDir=asc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();

        // Verify sorting (ascending by createdAt)
        var dates = result.Data.Select(q => q.CreatedAt).ToList();
        dates.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAdminQuotes_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?admin=true&status=Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(q => q.Status.Should().Be("Pending"));
    }

    [Fact]
    public async Task GetAdminQuotes_WithDateRange_ShouldFilterByDate()
    {
        // Arrange
        await SeedTestDataAsync();

        var utcToday = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        var dateFrom = LocalDatePattern.Iso.Format(utcToday.PlusDays(-7));
        var dateTo = LocalDatePattern.Iso.Format(utcToday.PlusDays(1));

        // Act
        var response = await Client.GetAsync($"/api/v1/quote/admin?admin=true&dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        var fromInclusive = LocalDatePattern.Iso.Parse(dateFrom).Value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        var toExclusive = LocalDatePattern.Iso.Parse(dateTo).Value.PlusDays(1).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();

        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(q =>
        {
            q.CreatedAt.Should().BeGreaterThanOrEqualTo(fromInclusive);
            q.CreatedAt.Should().BeLessThan(toExclusive);
        });
    }

    [Fact]
    public async Task GetAdminQuotes_WithInvalidPageSize_ShouldUseDefault()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?admin=true&pageSize=150"); // Exceeds max of 100

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, AdminQuoteJsonOptions);

        result.Should().NotBeNull();
        result!.Pagination.PageSize.Should().Be(100); // Should be capped at 100
    }

    [Fact]
    public async Task GetAdminQuotes_WithoutAdminFlag_ShouldReturnBadRequest()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/api/v1/quote/admin?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();

        // Clear existing data
        dbContext.Set<Quote>().RemoveRange(dbContext.Set<Quote>());
        var testSessionIds = new[] { "test-session-1", "test-session-2", "test-session-3" };
        var existingSessions = await dbContext.Set<QuoteSession>()
            .Where(s => testSessionIds.Contains(s.SessionId))
            .ToListAsync();
        dbContext.Set<QuoteSession>().RemoveRange(existingSessions);
        await dbContext.SaveChangesAsync();

        var now = SystemClock.Instance.GetCurrentInstant();

        dbContext.Set<QuoteSession>().AddRange(
            new QuoteSession
            {
                SessionId = "test-session-1",
                ETag = "seed-1",
                CreatedAt = now,
                ModifiedAt = now
            },
            new QuoteSession
            {
                SessionId = "test-session-2",
                ETag = "seed-2",
                CreatedAt = now,
                ModifiedAt = now
            },
            new QuoteSession
            {
                SessionId = "test-session-3",
                ETag = "seed-3",
                CreatedAt = now,
                ModifiedAt = now
            });
        await dbContext.SaveChangesAsync();

        // Create test quotes
        var quotes = new List<Quote>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = "test-session-1",
                Type = QuoteType.Send,
                QuoteReference = "TEST-REF-001",
                TotalCost = 150.00m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = now.Plus(Duration.FromDays(-1)),
                CreatedBy = "Test",
                ModifiedAt = now.Plus(Duration.FromDays(-1)),
                ModifiedBy = "Test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = "test-session-2",
                Type = QuoteType.Receive,
                QuoteReference = "TEST-REF-002",
                TotalCost = 200.00m,
                PaymentStatus = PaymentStatus.Succeeded,
                CreatedAt = now.Plus(Duration.FromDays(-2)),
                CreatedBy = "Test",
                ModifiedAt = now.Plus(Duration.FromDays(-2)),
                ModifiedBy = "Test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = "test-session-3",
                Type = QuoteType.Removals,
                QuoteReference = "TEST-REF-003",
                TotalCost = 300.00m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = now.Plus(Duration.FromDays(-3)),
                CreatedBy = "Test",
                ModifiedAt = now.Plus(Duration.FromDays(-3)),
                ModifiedBy = "Test"
            }
        };

        dbContext.Set<Quote>().AddRange(quotes);
        await dbContext.SaveChangesAsync();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
