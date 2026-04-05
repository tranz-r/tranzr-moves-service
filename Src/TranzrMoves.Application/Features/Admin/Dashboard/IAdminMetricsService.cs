using ErrorOr;

namespace TranzrMoves.Application.Features.Admin.Dashboard;

public interface IAdminMetricsService
{
    Task<ErrorOr<DashboardMetricsDto>> GetDashboardMetricsAsync(LocalDate? fromDate, LocalDate? toDate, CancellationToken ct);
}
