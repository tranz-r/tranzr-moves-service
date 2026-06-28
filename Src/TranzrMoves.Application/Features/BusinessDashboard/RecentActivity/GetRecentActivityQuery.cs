using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessDashboard.RecentActivity;

public sealed record GetRecentActivityQuery : IRequest<ErrorOr<IReadOnlyList<RecentActivityDto>>>;

public sealed class GetRecentActivityQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetRecentActivityQuery, ErrorOr<IReadOnlyList<RecentActivityDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<RecentActivityDto>>> Handle(
        GetRecentActivityQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "BusinessDashboard.Forbidden",
                description: "You do not have access to this business account.");
        }

        // TODO: Return the latest activity events for businessUser.BusinessAccountId once an
        // activity/audit source exists. Empty for now so the dashboard renders empty states (BR-006).
        return Array.Empty<RecentActivityDto>();
    }
}
