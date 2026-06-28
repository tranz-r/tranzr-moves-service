using Microsoft.AspNetCore.Authorization;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Authentication;

public sealed class BusinessUserRequirement : IAuthorizationRequirement;

public sealed class BusinessOwnerRequirement : IAuthorizationRequirement;

public sealed class BusinessAdminRequirement : IAuthorizationRequirement;

public sealed class BusinessFinanceRequirement : IAuthorizationRequirement;

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

public sealed class BusinessAdminAuthorizationHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : AuthorizationHandler<BusinessAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessAdminRequirement requirement)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(CancellationToken.None);
        if (businessUser is null
            || businessUser.Status != BusinessUserStatus.Active
            || businessUser.Role is not (BusinessUserRole.Owner or BusinessUserRole.Admin))
        {
            return;
        }

        context.Succeed(requirement);
    }
}

public sealed class BusinessFinanceAuthorizationHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : AuthorizationHandler<BusinessFinanceRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessFinanceRequirement requirement)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(CancellationToken.None);
        if (businessUser is null
            || businessUser.Status != BusinessUserStatus.Active
            || businessUser.Role is not (BusinessUserRole.Owner or BusinessUserRole.Admin or BusinessUserRole.Finance))
        {
            return;
        }

        context.Succeed(requirement);
    }
}
