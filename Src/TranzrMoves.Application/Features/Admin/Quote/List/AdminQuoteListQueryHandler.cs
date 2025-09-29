using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;

using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.List;

public class AdminQuoteListQueryHandler(
    IQuoteRepository quoteRepository,
    ILogger<AdminQuoteListQueryHandler> logger) : IQueryHandler<AdminQuoteListQuery, ErrorOr<AdminQuoteListResponse>>
{
    public async ValueTask<ErrorOr<AdminQuoteListResponse>> Handle(AdminQuoteListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination parameters
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Min(Math.Max(1, request.PageSize), 100); // Max 100 items per page

            // Get quotes from repository
            var (quotes, totalCount) = await quoteRepository.GetAdminQuotesAsync(
                page,
                pageSize,
                request.Search,
                request.SortBy,
                request.SortDir,
                request.Status,
                request.DateFrom,
                request.DateTo,
                cancellationToken);

            // Map to DTOs with efficient customer/driver name lookup
            var quoteDtos = MapToAdminQuoteDtos(quotes);

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var pagination = new PaginationMetadata(
                page,
                pageSize,
                totalCount,
                totalPages,
                page < totalPages,
                page > 1);

            return new AdminQuoteListResponse(quoteDtos, pagination);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving admin quotes list");
            return Error.Failure("AdminQuoteList.Failed", "Failed to retrieve quotes list");
        }
    }

    private static List<AdminQuoteDto> MapToAdminQuoteDtos(
        List<Domain.Entities.Quote> quotes)
    {
        var result = new List<AdminQuoteDto>();

        foreach (var quote in quotes)
        {
            // For admin listing, we show basic info and indicate that customer details are available
            // Full customer details will be fetched when viewing individual quote details
            var customerName = "Available";    // Indicates customer info is available in detail view

            // Calculate total cost including additional payments
            var baseCost = quote.TotalCost ?? 0;
            var additionalPaymentsTotal = quote.QuoteAdditionalPayments?.Sum(ap => ap.Amount) ?? 0;
            var totalCost = baseCost + additionalPaymentsTotal;

            // Determine status based on payment status
            var status = quote.PaymentStatus?.ToString() ?? "Pending";

            result.Add(new AdminQuoteDto(
                quote.Id,
                quote.QuoteReference,
                customerName,
                totalCost,
                status,
                quote.Type.ToString(),
                quote.PaymentType.ToString(),
                quote.CreatedAt));
        }

        return result;
    }
}
