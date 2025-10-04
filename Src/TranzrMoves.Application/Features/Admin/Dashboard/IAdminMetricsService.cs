using ErrorOr;

namespace TranzrMoves.Application.Features.Admin.Dashboard;

public interface IAdminMetricsService
{
    Task<ErrorOr<DashboardMetricsDto>> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct);
}
