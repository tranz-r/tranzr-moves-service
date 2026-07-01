using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessUser.Activate;
using TranzrMoves.Application.Features.BusinessUser.ChangeRole;
using TranzrMoves.Application.Features.BusinessUser.Deactivate;
using TranzrMoves.Application.Features.BusinessUser.Get;
using TranzrMoves.Application.Features.BusinessUser.Invitations;
using TranzrMoves.Application.Features.BusinessUser.Invite;
using TranzrMoves.Application.Features.BusinessUser.List;
using TranzrMoves.Application.Features.BusinessUser.Suspend;
using TranzrMoves.Application.Features.BusinessUser.TransferOwnership;
using TranzrMoves.Infrastructure.Authentication;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class BusinessUsersController(IMediator mediator, ILogger<BusinessUsersController> logger)
    : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_List",
        Summary = "List business users in the caller's tenant",
        Description = "Returns all business users belonging to the authenticated user's business account. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListBusinessUsersQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessUser_Get",
        Summary = "Get a business user by ID",
        Description = "Returns the business user when they belong to the authenticated user's tenant.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(BusinessUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBusinessUserQuery(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("invitations")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_Invite",
        Summary = "Invite a new business user",
        Description = "Creates an invited business user and sends a Supabase invitation email. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(InviteBusinessUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(
        [FromBody] InviteBusinessUserCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Inviting business user {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            response => CreatedAtAction(nameof(Get), new { id = response.BusinessUserId }, response),
            Problem);
    }

    [HttpGet("invitations")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_ListInvitations",
        Summary = "List pending invitations in the caller's tenant",
        Description = "Returns pending (invited) team invitations for the authenticated user's business account. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListInvitations(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListInvitationsQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("invitations/{id:guid}/revoke")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_RevokeInvitation",
        Summary = "Revoke a pending invitation",
        Description = "Cancels a pending invitation so its link can no longer be accepted. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(InvitationActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeInvitation(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RevokeInvitationCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("invitations/{id:guid}/resend")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_ResendInvitation",
        Summary = "Resend a pending invitation",
        Description = "Issues a fresh Supabase invite link and resets the expiry for a pending or expired invitation. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(InvitationActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ResendInvitationCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_ChangeRole",
        Summary = "Change a business user's role",
        Description = "Updates a member's role. Owners may assign any role except Owner; Admins may assign only Member, Finance or Viewer and cannot modify the Owner. Ownership is transferred via a dedicated endpoint.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(BusinessUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ChangeRoleCommand(id, request.Role), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/transfer-ownership")]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [SwaggerOperation(
        OperationId = "BusinessUser_TransferOwnership",
        Summary = "Transfer account ownership",
        Description = "Transfers ownership to another active member: the current Owner becomes Admin and the target becomes Owner. Requires Owner.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(TransferOwnershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOwnership(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new TransferOwnershipCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_Suspend",
        Summary = "Suspend a business user",
        Description = "Temporarily disables a business user's portal access. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(BusinessUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SuspendBusinessUserCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.BusinessAdmin)]
    [SwaggerOperation(
        OperationId = "BusinessUser_Activate",
        Summary = "Activate a business user",
        Description = "Reactivates an invited or suspended business user. Requires Owner or Admin.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(BusinessUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ActivateBusinessUserCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [SwaggerOperation(
        OperationId = "BusinessUser_Deactivate",
        Summary = "Deactivate a business user",
        Description = "Permanently disables a business user while retaining historical data. Requires Owner.",
        Tags = ["Business Users (v1)"])]
    [ProducesResponseType(typeof(BusinessUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateBusinessUserCommand(id), cancellationToken);
        return result.Match(Ok, Problem);
    }
}
