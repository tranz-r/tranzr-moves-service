using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Auth.Context;

public sealed record GetAuthContextQuery : IRequest<ErrorOr<AuthContextDto>>;

public sealed class GetAuthContextQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetAuthContextQuery, ErrorOr<AuthContextDto>>
{
    public async ValueTask<ErrorOr<AuthContextDto>> Handle(
        GetAuthContextQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "Auth.Forbidden",
                description: "You do not have access to the Business Portal.");
        }

        return new AuthContextDto
        {
            UserId = businessUser.UserId,
            BusinessUserId = businessUser.Id,
            BusinessAccountId = businessUser.BusinessAccountId,
            FirstName = businessUser.User?.FirstName,
            LastName = businessUser.User?.LastName,
            Email = businessUser.User?.Email ?? currentBusinessUserContext.Email,
            Role = businessUser.Role,
        };
    }
}
