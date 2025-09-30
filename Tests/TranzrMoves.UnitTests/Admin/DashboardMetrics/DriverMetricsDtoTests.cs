using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class DriverMetricsDtoTests
{
    [Fact]
    public void DriverMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new DriverMetricsDto
        {
            Total = 25,
            Active = 20,
            Available = 15,
            Busy = 5,
            UtilizationRate = 25.0,
            AverageAssignmentsPerDriver = 4.8
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"total\": 25");
        json.Should().Contain("\"active\": 20");
        json.Should().Contain("\"available\": 15");
        json.Should().Contain("\"busy\": 5");
        json.Should().Contain("\"utilizationRate\": 25");
        json.Should().Contain("\"averageAssignmentsPerDriver\": 4.8");
    }

    [Fact]
    public void DriverMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new DriverMetricsDto();

        // Assert
        dto.Total.Should().Be(0);
        dto.Active.Should().Be(0);
        dto.Available.Should().Be(0);
        dto.Busy.Should().Be(0);
        dto.UtilizationRate.Should().Be(0);
        dto.AverageAssignmentsPerDriver.Should().Be(0);
    }
}
