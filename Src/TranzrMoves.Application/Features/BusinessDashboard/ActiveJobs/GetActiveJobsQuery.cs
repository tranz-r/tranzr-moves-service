using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessDashboard.ActiveJobs;

public sealed record GetActiveJobsQuery : IRequest<ErrorOr<IReadOnlyList<ActiveJobDto>>>;

public sealed class GetActiveJobsQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetActiveJobsQuery, ErrorOr<IReadOnlyList<ActiveJobDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<ActiveJobDto>>> Handle(
        GetActiveJobsQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "BusinessDashboard.Forbidden",
                description: "You do not have access to this business account.");
        }

        // TODO: Return the in-progress jobs for businessUser.BusinessAccountId once the
        // Business Booking feature lands. Empty for now so the dashboard renders empty states (BR-006).
        return Array.Empty<ActiveJobDto>();
    }
}
