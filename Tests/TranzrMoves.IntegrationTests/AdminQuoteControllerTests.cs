using System.Net;
using System.Text.Json;
using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests;

public class AdminQuoteControllerTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;

    public AdminQuoteControllerTests(TestingWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAdminQuotes_WithValidParameters_ShouldReturnPaginatedResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/v1/quote?admin=true&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/v1/quote?admin=true&search=TEST-REF-001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.First().QuoteReference.Should().Contain("TEST-REF-001");
    }

    [Fact]
    public async Task GetAdminQuotes_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/v1/quote?admin=true&sortBy=createdAt&sortDir=asc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/v1/quote?admin=true&status=Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(q => q.Status.Should().Be("Pending"));
    }

    [Fact]
    public async Task GetAdminQuotes_WithDateRange_ShouldFilterByDate()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        var dateFrom = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        // Act
        var response = await client.GetAsync($"/api/v1/quote?admin=true&dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(q =>
            q.CreatedAt.Should().BeAfter(DateTimeOffset.Parse(dateFrom))
             .And.BeBefore(DateTimeOffset.Parse(dateTo)));
    }

    [Fact]
    public async Task GetAdminQuotes_WithInvalidPageSize_ShouldUseDefault()
    {
        // Arrange
        var client = _factory.CreateClient();
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/v1/quote?admin=true&pageSize=150"); // Exceeds max of 100

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AdminQuoteListResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Pagination.PageSize.Should().Be(100); // Should be capped at 100
    }

    [Fact]
    public async Task GetAdminQuotes_WithoutAdminFlag_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/quote?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();

        // Clear existing data
        dbContext.Set<Quote>().RemoveRange(dbContext.Set<Quote>());
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
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedBy = "Test",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1),
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
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                CreatedBy = "Test",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-2),
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
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
                CreatedBy = "Test",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-3),
                ModifiedBy = "Test"
            }
        };

        dbContext.Set<Quote>().AddRange(quotes);
        await dbContext.SaveChangesAsync();
    }
}

// Response DTOs for testing
public record AdminQuoteListResponse(
    List<AdminQuoteDto> Data,
    PaginationMetadata Pagination);

public record AdminQuoteDto(
    Guid Id,
    string QuoteReference,
    string CustomerName,
    decimal? TotalCost,
    string Status,
    DateTimeOffset CreatedAt,
    string? DriverName,
    Guid? DriverId);

public record PaginationMetadata(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);
