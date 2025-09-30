using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TranzrMoves.Application.Features.Admin.Dashboard;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class AdminMetricsServiceTests : IDisposable
{
    private readonly TranzrMovesDbContext _context;
    private readonly ILogger<AdminMetricsService> _loggerMock;
    private readonly AdminMetricsService _service;

    public AdminMetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<TranzrMovesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TranzrMovesDbContext(options);
        _loggerMock = Substitute.For<ILogger<AdminMetricsService>>();
        _service = new AdminMetricsService(_context, _loggerMock);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_ShouldReturnCompleteMetrics()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        var metrics = result.Value;
        metrics.Quotes.Should().NotBeNull();
        metrics.Payments.Should().NotBeNull();
        metrics.Users.Should().NotBeNull();
        metrics.Drivers.Should().NotBeNull();
        metrics.Revenue.Should().NotBeNull();
        metrics.Operational.Should().NotBeNull();
        metrics.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        metrics.CacheExpiry.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithDateRange_ShouldFilterCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _service.GetDashboardMetricsAsync(fromDate, toDate, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        // Should only include data within the date range
        result.Value.Quotes.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetQuoteMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Quotes.Should().NotBeNull();
        result.Value.Quotes.Total.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Quotes.ByType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Payments.Should().NotBeNull();
        result.Value.Payments.TotalAmount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Payments.ByPaymentType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Users.Should().NotBeNull();
        result.Value.Users.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetDriverMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Drivers.Should().NotBeNull();
        result.Value.Drivers.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetRevenueMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Revenue.Should().NotBeNull();
        result.Value.Revenue.Total.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Revenue.ByServiceType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOperationalMetricsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardMetricsAsync(null, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Operational.Should().NotBeNull();
        result.Value.Operational.AverageQuoteValue.Should().BeGreaterThanOrEqualTo(0);
    }

    private async Task SeedTestDataAsync()
    {
        // Add test users
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "customer1@test.com", Role = Role.customer, CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { Id = Guid.NewGuid(), Email = "driver1@test.com", Role = Role.driver, CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { Id = Guid.NewGuid(), Email = "admin1@test.com", Role = Role.admin, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
        };

        _context.Set<User>().AddRange(users);

        // Add test quotes
        var quotes = new List<Quote>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = QuoteType.Send,
                PaymentStatus = PaymentStatus.Succeeded,
                PaymentType = PaymentType.Full,
                TotalCost = 1000m,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = QuoteType.Receive,
                PaymentStatus = PaymentStatus.Pending,
                PaymentType = PaymentType.Deposit,
                TotalCost = 500m,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = QuoteType.Removals,
                PaymentStatus = PaymentStatus.Paid,
                PaymentType = PaymentType.Later,
                TotalCost = 750m,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _context.Set<Quote>().AddRange(quotes);

        // Add driver quotes
        var driverQuotes = new List<DriverQuote>
        {
            new() { Id = Guid.NewGuid(), UserId = users[1].Id, QuoteId = quotes[0].Id },
            new() { Id = Guid.NewGuid(), UserId = users[1].Id, QuoteId = quotes[1].Id }
        };

        _context.Set<DriverQuote>().AddRange(driverQuotes);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
