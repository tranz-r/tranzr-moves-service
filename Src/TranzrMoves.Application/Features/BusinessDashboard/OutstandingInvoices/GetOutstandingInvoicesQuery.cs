using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessDashboard.OutstandingInvoices;

public sealed record GetOutstandingInvoicesQuery : IRequest<ErrorOr<IReadOnlyList<OutstandingInvoiceDto>>>;

public sealed class GetOutstandingInvoicesQueryHandler(
    ICurrentBusinessUserContext currentBusinessUserContext)
    : IRequestHandler<GetOutstandingInvoicesQuery, ErrorOr<IReadOnlyList<OutstandingInvoiceDto>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<OutstandingInvoiceDto>>> Handle(
        GetOutstandingInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var businessUser = await currentBusinessUserContext.GetBusinessUserAsync(cancellationToken);
        if (businessUser is null || businessUser.Status != BusinessUserStatus.Active)
        {
            return Error.Forbidden(
                code: "BusinessDashboard.Forbidden",
                description: "You do not have access to this business account.");
        }

        // TODO: Return outstanding invoices for businessUser.BusinessAccountId once the
        // Invoice Centre feature lands. Empty for now so the dashboard renders empty states (BR-006).
        return Array.Empty<OutstandingInvoiceDto>();
    }
}
