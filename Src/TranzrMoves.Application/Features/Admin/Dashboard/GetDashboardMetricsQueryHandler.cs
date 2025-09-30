using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;

namespace TranzrMoves.Application.Features.Admin.Dashboard;

/// <summary>
/// Handler for retrieving dashboard metrics
/// </summary>
public class GetDashboardMetricsQueryHandler : IQueryHandler<GetDashboardMetricsQuery, ErrorOr<DashboardMetricsDto>>
{
    private readonly IAdminMetricsService _metricsService;
    private readonly ILogger<GetDashboardMetricsQueryHandler> _logger;

    public GetDashboardMetricsQueryHandler(
        IAdminMetricsService metricsService,
        ILogger<GetDashboardMetricsQueryHandler> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    public async ValueTask<ErrorOr<DashboardMetricsDto>> Handle(GetDashboardMetricsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving dashboard metrics for date range: {FromDate} to {ToDate}",
                query.FromDate, query.ToDate);

            var result = await _metricsService.GetDashboardMetricsAsync(query.FromDate, query.ToDate, cancellationToken);

            if (result.IsError)
            {
                _logger.LogError("Failed to retrieve dashboard metrics: {Error}", result.FirstError.Description);
                return result.Errors;
            }

            _logger.LogInformation("Successfully retrieved dashboard metrics");
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics for date range: {FromDate} to {ToDate}",
                query.FromDate, query.ToDate);
            return Error.Unexpected("DashboardMetrics.Failed", "Failed to retrieve dashboard metrics.");
        }
    }
}
