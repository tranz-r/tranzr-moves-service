using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class OperationalMetricsDtoTests
{
    [Fact]
    public void OperationalMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new OperationalMetricsDto
        {
            AverageQuoteValue = 833.33m,
            QuotesCompletedThisMonth = 45,
            AverageCompletionTime = 2.5,
            CustomerSatisfactionScore = 4.2
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"averageQuoteValue\": 833.33");
        json.Should().Contain("\"quotesCompletedThisMonth\": 45");
        json.Should().Contain("\"averageCompletionTime\": 2.5");
        json.Should().Contain("\"customerSatisfactionScore\": 4.2");
    }

    [Fact]
    public void OperationalMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new OperationalMetricsDto();

        // Assert
        dto.AverageQuoteValue.Should().Be(0);
        dto.QuotesCompletedThisMonth.Should().Be(0);
        dto.AverageCompletionTime.Should().Be(0);
        dto.CustomerSatisfactionScore.Should().Be(0);
    }
}
