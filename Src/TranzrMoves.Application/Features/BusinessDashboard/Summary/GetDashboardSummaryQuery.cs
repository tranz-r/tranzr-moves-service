using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessDashboard.Summary;

public sealed record GetDashboardSummaryQuery : IRequest<ErrorOr<DashboardSummaryDto>>;

public sealed class GetDashboardSummaryQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetDashboardSummaryQuery, ErrorOr<DashboardSummaryDto>>
{
    public async ValueTask<ErrorOr<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "BusinessDashboard.Forbidden",
                description: "You do not have access to this business account.");
        }

        // TODO: Populate from business-scoped Booking and Invoice data once those features land.
        // Until then the dashboard returns zeros so it renders empty states (BR-006), scoped to
        // businessUser.BusinessAccountId.
        return new DashboardSummaryDto
        {
            UpcomingJobs = 0,
            JobsInProgress = 0,
            CompletedJobsThisMonth = 0,
            OutstandingInvoiceAmount = 0m,
        };
    }
}
