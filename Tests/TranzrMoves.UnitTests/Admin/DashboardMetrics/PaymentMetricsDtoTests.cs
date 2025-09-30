using System.Text.Json;
using FluentAssertions;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class PaymentMetricsDtoTests
{
    [Fact]
    public void PaymentMetricsDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new PaymentMetricsDto
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
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"totalAmount\": 125000.00");
        json.Should().Contain("\"completedAmount\": 100000.00");
        json.Should().Contain("\"pendingAmount\": 25000.00");
        json.Should().Contain("\"totalTransactions\": 120");
        json.Should().Contain("\"successRate\": 95.5");
        json.Should().Contain("\"averageOrderValue\": 1041.67");
        json.Should().Contain("\"byPaymentType\"");
    }

    [Fact]
    public void PaymentTypeBreakdownDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new PaymentTypeBreakdownDto
        {
            Full = 60,
            Deposit = 40,
            Later = 15,
            Balance = 5
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"full\": 60");
        json.Should().Contain("\"deposit\": 40");
        json.Should().Contain("\"later\": 15");
        json.Should().Contain("\"balance\": 5");
    }

    [Fact]
    public void PaymentMetricsDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new PaymentMetricsDto();

        // Assert
        dto.TotalAmount.Should().Be(0);
        dto.CompletedAmount.Should().Be(0);
        dto.PendingAmount.Should().Be(0);
        dto.TotalTransactions.Should().Be(0);
        dto.SuccessRate.Should().Be(0);
        dto.AverageOrderValue.Should().Be(0);
        dto.ByPaymentType.Should().NotBeNull();
    }

    [Fact]
    public void PaymentTypeBreakdownDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new PaymentTypeBreakdownDto();

        // Assert
        dto.Full.Should().Be(0);
        dto.Deposit.Should().Be(0);
        dto.Later.Should().Be(0);
        dto.Balance.Should().Be(0);
    }
}
