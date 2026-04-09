using Mediator;
using Microsoft.Extensions.Logging;

namespace TranzrMoves.Application.Features.Admin.Dashboard;

/// <summary>
/// Handler for retrieving dashboard metrics
/// </summary>
internal sealed class GetDashboardMetricsQueryHandler(IAdminMetricsService metricsService, ILogger<GetDashboardMetricsQueryHandler> logger) : IQueryHandler<GetDashboardMetricsQuery, ErrorOr<DashboardMetricsDto>>
{
    public async ValueTask<ErrorOr<DashboardMetricsDto>> Handle(GetDashboardMetricsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving dashboard metrics for date range: {FromDate} to {ToDate}",
                query.FromDate, query.ToDate);

            var result = await metricsService.GetDashboardMetricsAsync(query.FromDate, query.ToDate, cancellationToken);

            if (result.IsError)
            {
                logger.LogError("Failed to retrieve dashboard metrics: {Error}", result.FirstError.Description);
                return result.Errors;
            }

            logger.LogInformation("Successfully retrieved dashboard metrics");
            return result.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard metrics for date range: {FromDate} to {ToDate}",
                query.FromDate, query.ToDate);
            return Error.Unexpected("DashboardMetrics.Failed", "Failed to retrieve dashboard metrics.");
        }
    }
}
