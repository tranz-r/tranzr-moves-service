using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class DashboardMetricsDtoTests
{
    [Fact]
    public void DashboardMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new DashboardMetricsDto
        {
            Quotes = new QuoteMetricsDto
            {
                Total = 150,
                Pending = 25,
                Paid = 100,
                PartiallyPaid = 15,
                Succeeded = 8,
                Cancelled = 2,
                ByType = new QuoteTypeBreakdownDto
                {
                    Send = 80,
                    Receive = 45,
                    Removals = 25
                }
            },
            Payments = new PaymentMetricsDto
            {
                TotalAmount = 125000.00m,
                CompletedAmount = 100000.00m,
                PendingAmount = 25000.00m,
                TotalTransactions = 120,
                SuccessRate = 95.5,
                AverageOrderValue = 1041.67m,
                ByPaymentType = new PaymentTypeBreakdownDto
                {
                    Full = 60,
                    Deposit = 40,
                    Later = 15,
                    Balance = 5
                }
            },
            Users = new UserMetricsDto
            {
                Total = 75,
                Customers = 60,
                Drivers = 12,
                Admins = 3,
                NewThisMonth = 10,
                GrowthRate = 15.4
            },
            Drivers = new DriverMetricsDto
            {
                Total = 25,
                Active = 20,
                Available = 15,
                Busy = 5,
                UtilizationRate = 25.0,
                AverageAssignmentsPerDriver = 4.8
            },
            Revenue = new RevenueMetricsDto
            {
                Total = 125000.00m,
                ThisMonth = 15000.00m,
                LastMonth = 12000.00m,
                MonthOverMonthGrowth = 25.0,
                ByServiceType = new ServiceTypeRevenueDto
                {
                    Send = 80000.00m,
                    Receive = 30000.00m,
                    Removals = 15000.00m
                }
            },
            Operational = new OperationalMetricsDto
            {
                AverageQuoteValue = 833.33m,
                QuotesCompletedThisMonth = 45,
                AverageCompletionTime = 2.5,
                CustomerSatisfactionScore = 4.2
            },
            LastUpdated = DateTimeOffset.UtcNow,
            CacheExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"quotes\"");
        json.Should().Contain("\"payments\"");
        json.Should().Contain("\"users\"");
        json.Should().Contain("\"drivers\"");
        json.Should().Contain("\"revenue\"");
        json.Should().Contain("\"operational\"");
        json.Should().Contain("\"lastUpdated\"");
        json.Should().Contain("\"cacheExpiry\"");
    }

    [Fact]
    public void DashboardMetricsDto_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "quotes": {
                "total": 150,
                "pending": 25,
                "paid": 100,
                "partiallyPaid": 15,
                "succeeded": 8,
                "cancelled": 2,
                "byType": {
                    "send": 80,
                    "receive": 45,
                    "removals": 25
                }
            },
            "payments": {
                "totalAmount": 125000.00,
                "completedAmount": 100000.00,
                "pendingAmount": 25000.00,
                "totalTransactions": 120,
                "successRate": 95.5,
                "averageOrderValue": 1041.67,
                "byPaymentType": {
                    "full": 60,
                    "deposit": 40,
                    "later": 15,
                    "balance": 5
                }
            },
            "users": {
                "total": 75,
                "customers": 60,
                "drivers": 12,
                "admins": 3,
                "newThisMonth": 10,
                "growthRate": 15.4
            },
            "drivers": {
                "total": 25,
                "active": 20,
                "available": 15,
                "busy": 5,
                "utilizationRate": 25.0,
                "averageAssignmentsPerDriver": 4.8
            },
            "revenue": {
                "total": 125000.00,
                "thisMonth": 15000.00,
                "lastMonth": 12000.00,
                "monthOverMonthGrowth": 25.0,
                "byServiceType": {
                    "send": 80000.00,
                    "receive": 30000.00,
                    "removals": 15000.00
                }
            },
            "operational": {
                "averageQuoteValue": 833.33,
                "quotesCompletedThisMonth": 45,
                "averageCompletionTime": 2.5,
                "customerSatisfactionScore": 4.2
            },
            "lastUpdated": "2025-01-27T10:30:00Z",
            "cacheExpiry": "2025-01-27T10:35:00Z"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<DashboardMetricsDto>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        dto.Should().NotBeNull();
        dto!.Quotes.Total.Should().Be(150);
        dto.Payments.TotalAmount.Should().Be(125000.00m);
        dto.Users.Total.Should().Be(75);
        dto.Drivers.Total.Should().Be(25);
        dto.Revenue.Total.Should().Be(125000.00m);
        dto.Operational.AverageQuoteValue.Should().Be(833.33m);
    }

    [Fact]
    public void DashboardMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new DashboardMetricsDto();

        // Assert
        dto.Quotes.Should().NotBeNull();
        dto.Payments.Should().NotBeNull();
        dto.Users.Should().NotBeNull();
        dto.Drivers.Should().NotBeNull();
        dto.Revenue.Should().NotBeNull();
        dto.Operational.Should().NotBeNull();
        dto.LastUpdated.Should().Be(default(DateTimeOffset));
        dto.CacheExpiry.Should().Be(default(DateTimeOffset));
    }
}
