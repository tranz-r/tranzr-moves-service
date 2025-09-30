using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.Admin.Dashboard;

/// <summary>
/// Query to retrieve dashboard metrics for admin panel
/// </summary>
/// <param name="FromDate">Optional start date for filtering metrics</param>
/// <param name="ToDate">Optional end date for filtering metrics</param>
public record GetDashboardMetricsQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<ErrorOr<DashboardMetricsDto>>;
