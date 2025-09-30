using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class RevenueMetricsDtoTests
{
    [Fact]
    public void RevenueMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new RevenueMetricsDto
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
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"total\": 125000.00");
        json.Should().Contain("\"thisMonth\": 15000.00");
        json.Should().Contain("\"lastMonth\": 12000.00");
        json.Should().Contain("\"monthOverMonthGrowth\": 25");
        json.Should().Contain("\"byServiceType\"");
    }

    [Fact]
    public void ServiceTypeRevenueDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new ServiceTypeRevenueDto
        {
            Send = 80000.00m,
            Receive = 30000.00m,
            Removals = 15000.00m
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"send\": 80000.00");
        json.Should().Contain("\"receive\": 30000.00");
        json.Should().Contain("\"removals\": 15000.00");
    }

    [Fact]
    public void RevenueMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new RevenueMetricsDto();

        // Assert
        dto.Total.Should().Be(0);
        dto.ThisMonth.Should().Be(0);
        dto.LastMonth.Should().Be(0);
        dto.MonthOverMonthGrowth.Should().Be(0);
        dto.ByServiceType.Should().NotBeNull();
    }

    [Fact]
    public void ServiceTypeRevenueDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new ServiceTypeRevenueDto();

        // Assert
        dto.Send.Should().Be(0);
        dto.Receive.Should().Be(0);
        dto.Removals.Should().Be(0);
    }
}
