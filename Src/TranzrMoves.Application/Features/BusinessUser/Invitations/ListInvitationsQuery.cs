using Mediator;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessUser.Invitations;

public sealed record ListInvitationsQuery : IRequest<ErrorOr<IReadOnlyList<InvitationDto>>>;

public sealed class ListInvitationsQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessUserRepository businessUserRepository,
    ITimeService timeService)
    : IRequestHandler<ListInvitationsQuery, ErrorOr<IReadOnlyList<InvitationDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<InvitationDto>>> Handle(
        ListInvitationsQuery query,
        CancellationToken cancellationToken)
    {
        var caller = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (caller is null)
        {
            return Error.Forbidden(
                code: "BusinessUser.Forbidden",
                description: "You do not have access to this business account.");
        }

        var members = await businessUserRepository.GetByBusinessAccountIdAsync(
            caller.BusinessAccountId,
            cancellationToken);

        // Inviter name lookup: the inviter is another BusinessUser within the same tenant.
        var nameById = members.ToDictionary(m => m.Id, m => DisplayName(m.User));
        var now = timeService.Now();

        var invitations = members
            .Where(m => m.Status == BusinessUserStatus.Invited)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new InvitationDto
            {
                BusinessUserId = m.Id,
                FirstName = m.User?.FirstName,
                LastName = m.User?.LastName,
                Email = m.User?.Email,
                Role = m.Role,
                Status = m.Status,
                InvitedByName = m.CreatedByBusinessUserId is { } inviterId && nameById.TryGetValue(inviterId, out var name)
                    ? name
                    : null,
                SentAtUtc = m.CreatedAt,
                ExpiresAtUtc = m.InvitationExpiresAt,
                IsExpired = m.InvitationExpiresAt is { } expiry && expiry < now,
            })
            .ToList();

        return invitations;
    }

    private static string? DisplayName(UserV2? user)
    {
        if (user is null)
        {
            return null;
        }

        var full = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? user.Email : full;
    }
}
