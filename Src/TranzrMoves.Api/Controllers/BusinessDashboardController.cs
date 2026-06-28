using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessDashboard.ActiveJobs;
using TranzrMoves.Application.Features.BusinessDashboard.OutstandingInvoices;
using TranzrMoves.Application.Features.BusinessDashboard.RecentActivity;
using TranzrMoves.Application.Features.BusinessDashboard.Summary;
using TranzrMoves.Application.Features.BusinessDashboard.UpcomingJobs;
using TranzrMoves.Infrastructure.Authentication;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class BusinessDashboardController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessDashboard_GetSummary",
        Summary = "Get the dashboard KPI summary",
        Description = "Returns upcoming jobs, jobs in progress, completed jobs this month, and outstanding invoice amount, scoped to the authenticated Business Account.",
        Tags = ["Business Dashboard (v1)"])]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("upcoming-jobs")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessDashboard_GetUpcomingJobs",
        Summary = "Get upcoming jobs",
        Description = "Returns the next upcoming jobs for the authenticated Business Account.",
        Tags = ["Business Dashboard (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<UpcomingJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUpcomingJobs(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUpcomingJobsQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("active-jobs")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessDashboard_GetActiveJobs",
        Summary = "Get active (in-progress) jobs",
        Description = "Returns all active jobs for the authenticated Business Account.",
        Tags = ["Business Dashboard (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveJobs(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetActiveJobsQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("outstanding-invoices")]
    [Authorize(Policy = AuthorizationPolicies.BusinessFinance)]
    [SwaggerOperation(
        OperationId = "BusinessDashboard_GetOutstandingInvoices",
        Summary = "Get outstanding invoices",
        Description = "Returns outstanding invoices for the authenticated Business Account. Requires Owner, Admin, or Finance.",
        Tags = ["Business Dashboard (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<OutstandingInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOutstandingInvoices(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOutstandingInvoicesQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("recent-activity")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessDashboard_GetRecentActivity",
        Summary = "Get recent activity",
        Description = "Returns the latest activity events for the authenticated Business Account.",
        Tags = ["Business Dashboard (v1)"])]
    [ProducesResponseType(typeof(IReadOnlyList<RecentActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentActivity(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecentActivityQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }
}
