using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class QuoteMetricsDtoTests
{
    [Fact]
    public void QuoteMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new QuoteMetricsDto
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
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"total\": 150");
        json.Should().Contain("\"pending\": 25");
        json.Should().Contain("\"paid\": 100");
        json.Should().Contain("\"partiallyPaid\": 15");
        json.Should().Contain("\"succeeded\": 8");
        json.Should().Contain("\"cancelled\": 2");
        json.Should().Contain("\"byType\"");
    }

    [Fact]
    public void QuoteTypeBreakdownDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new QuoteTypeBreakdownDto
        {
            Send = 80,
            Receive = 45,
            Removals = 25
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"send\": 80");
        json.Should().Contain("\"receive\": 45");
        json.Should().Contain("\"removals\": 25");
    }

    [Fact]
    public void QuoteMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new QuoteMetricsDto();

        // Assert
        dto.Total.Should().Be(0);
        dto.Pending.Should().Be(0);
        dto.Paid.Should().Be(0);
        dto.PartiallyPaid.Should().Be(0);
        dto.Succeeded.Should().Be(0);
        dto.Cancelled.Should().Be(0);
        dto.ByType.Should().NotBeNull();
    }

    [Fact]
    public void QuoteTypeBreakdownDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new QuoteTypeBreakdownDto();

        // Assert
        dto.Send.Should().Be(0);
        dto.Receive.Should().Be(0);
        dto.Removals.Should().Be(0);
    }
}
