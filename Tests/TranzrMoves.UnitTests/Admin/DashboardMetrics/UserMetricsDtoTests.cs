using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class UserMetricsDtoTests
{
    [Fact]
    public void UserMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new UserMetricsDto
        {
            Total = 75,
            Customers = 60,
            Drivers = 12,
            Admins = 3,
            NewThisMonth = 10,
            GrowthRate = 15.4
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"total\": 75");
        json.Should().Contain("\"customers\": 60");
        json.Should().Contain("\"drivers\": 12");
        json.Should().Contain("\"admins\": 3");
        json.Should().Contain("\"newThisMonth\": 10");
        json.Should().Contain("\"growthRate\": 15.4");
    }

    [Fact]
    public void UserMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new UserMetricsDto();

        // Assert
        dto.Total.Should().Be(0);
        dto.Customers.Should().Be(0);
        dto.Drivers.Should().Be(0);
        dto.Admins.Should().Be(0);
        dto.NewThisMonth.Should().Be(0);
        dto.GrowthRate.Should().Be(0);
    }
}
