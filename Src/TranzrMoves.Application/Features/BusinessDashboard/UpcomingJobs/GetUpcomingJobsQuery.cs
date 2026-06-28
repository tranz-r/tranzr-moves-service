using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessDashboard.UpcomingJobs;

public sealed record GetUpcomingJobsQuery : IRequest<ErrorOr<IReadOnlyList<UpcomingJobDto>>>;

public sealed class GetUpcomingJobsQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetUpcomingJobsQuery, ErrorOr<IReadOnlyList<UpcomingJobDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<UpcomingJobDto>>> Handle(
        GetUpcomingJobsQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "BusinessDashboard.Forbidden",
                description: "You do not have access to this business account.");
        }

        // TODO: Return the next upcoming jobs for businessUser.BusinessAccountId once the
        // Business Booking feature lands. Empty for now so the dashboard renders empty states (BR-006).
        return Array.Empty<UpcomingJobDto>();
    }
}
