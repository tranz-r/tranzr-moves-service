using ErrorOr;
using FluentAssertions;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using NSubstitute;
using TranzrMoves.Api.Controllers;
using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.UnitTests.Admin.DashboardMetrics;

public class AdminControllerTests
{
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AdminController> _logger;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _memoryCache = Substitute.For<IMemoryCache>();
        _logger = Substitute.For<ILogger<AdminController>>();
        _controller = new AdminController(_mediator, _memoryCache, _logger)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetDashboardMetrics_ShouldReturnCachedResult_WhenCacheExists()
    {
        // Arrange
        var cachedMetrics = new DashboardMetricsDto
        {
            Quotes = new QuoteMetricsDto { Total = 10 },
            Payments = new PaymentMetricsDto { TotalAmount = 1000 },
            Users = new UserMetricsDto { Total = 5 },
            Drivers = new DriverMetricsDto { Total = 3 },
            Revenue = new RevenueMetricsDto { Total = 5000 },
            Operational = new OperationalMetricsDto { AverageQuoteValue = 500 },
            LastUpdated = SystemClock.Instance.GetCurrentInstant(),
            CacheExpiry = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(5))
        };

        var cacheKey = "admin_dashboard_metrics";
        _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>())
            .Returns(x =>
            {
                x[1] = cachedMetrics;
                return true;
            });

        // Act
        var result = await _controller.GetDashboardMetrics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(cachedMetrics);

        // Verify mediator was not called
        await _mediator.DidNotReceive().Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardMetrics_ShouldCallService_WhenCacheMiss()
    {
        // Arrange
        var serviceMetrics = new DashboardMetricsDto
        {
            Quotes = new QuoteMetricsDto { Total = 15 },
            Payments = new PaymentMetricsDto { TotalAmount = 2000 },
            Users = new UserMetricsDto { Total = 8 },
            Drivers = new DriverMetricsDto { Total = 4 },
            Revenue = new RevenueMetricsDto { Total = 8000 },
            Operational = new OperationalMetricsDto { AverageQuoteValue = 600 },
            LastUpdated = SystemClock.Instance.GetCurrentInstant(),
            CacheExpiry = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(5))
        };

        var cacheKey = "admin_dashboard_metrics";
        _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>()).Returns(false);

        _mediator.Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<ErrorOr<DashboardMetricsDto>>(serviceMetrics));

        // Act
        var result = await _controller.GetDashboardMetrics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(serviceMetrics);

        // Verify mediator was called
        await _mediator.Received(1).Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardMetrics_WithDateRange_ShouldPassParametersToService()
    {
        // Arrange
        var toLd = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        var fromLd = toLd.PlusDays(-30);
        var serviceMetrics = new DashboardMetricsDto
        {
            Quotes = new QuoteMetricsDto { Total = 5 },
            Payments = new PaymentMetricsDto { TotalAmount = 1000 },
            Users = new UserMetricsDto { Total = 2 },
            Drivers = new DriverMetricsDto { Total = 1 },
            Revenue = new RevenueMetricsDto { Total = 2000 },
            Operational = new OperationalMetricsDto { AverageQuoteValue = 400 },
            LastUpdated = SystemClock.Instance.GetCurrentInstant(),
            CacheExpiry = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(5))
        };

        var cacheKey =
            $"admin_dashboard_metrics_{LocalDatePattern.Iso.Format(fromLd)}_{LocalDatePattern.Iso.Format(toLd)}";
        _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>()).Returns(false);

        _mediator.Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<ErrorOr<DashboardMetricsDto>>(serviceMetrics));

        // Act
        var result = await _controller.GetDashboardMetrics(fromLd, toLd);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(serviceMetrics);

        // Verify mediator was called with correct parameters
        await _mediator.Received(1).Send(Arg.Is<GetDashboardMetricsQuery>(q => q.FromDate == fromLd && q.ToDate == toLd), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardMetrics_ShouldReturnBadRequest_WhenDateRangeInvalid()
    {
        // Arrange
        var mid = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        var fromLd = mid;
        var toLd = mid.PlusDays(-30); // toDate is before fromDate

        // Act
        var result = await _controller.GetDashboardMetrics(fromLd, toLd);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("From date must be before or equal to to date");

        // Verify mediator was not called
        await _mediator.DidNotReceive().Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardMetrics_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        var cacheKey = "admin_dashboard_metrics";
        _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>()).Returns(false);

        _mediator.Send(Arg.Any<GetDashboardMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<ErrorOr<DashboardMetricsDto>>(new Exception("Database connection failed")));

        // Act
        var result = await _controller.GetDashboardMetrics(null, null);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().Be("An error occurred while retrieving dashboard metrics");
    }

}
