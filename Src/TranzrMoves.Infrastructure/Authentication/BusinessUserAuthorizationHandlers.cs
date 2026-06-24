using Microsoft.AspNetCore.Authorization;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Authentication;

public sealed class BusinessUserRequirement : IAuthorizationRequirement;

public sealed class BusinessOwnerRequirement : IAuthorizationRequirement;

public sealed class BusinessUserAuthorizationHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : AuthorizationHandler<BusinessUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessUserRequirement requirement)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(CancellationToken.None);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return;
        }

        context.Succeed(requirement);
    }
}

public sealed class BusinessOwnerAuthorizationHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : AuthorizationHandler<BusinessOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessOwnerRequirement requirement)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(CancellationToken.None);
        if (businessUser is null
            || businessUser.Status != BusinessUserStatus.Active
            || businessUser.Role != BusinessUserRole.Owner)
        {
            return;
        }

        context.Succeed(requirement);
    }
}
