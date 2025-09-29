using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.Admin.Quote.List;

public record AdminQuoteListQuery(
    int Page = 1,
    int PageSize = 50,
    string? Search = null,
    string? SortBy = "createdAt",
    string? SortDir = "desc",
    string? Status = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<ErrorOr<AdminQuoteListResponse>>;

public record AdminQuoteListResponse(
    List<AdminQuoteDto> Data,
    PaginationMetadata Pagination);

public record AdminQuoteDto(
    Guid Id,
    string QuoteReference,
    string CustomerName,      // "Available" for listing, full name in detail view
    decimal? TotalCost,
    string Status,
    string QuoteType,         // Send, Receive, Removals
    string PaymentType,      // Card, BankTransfer, Cash, etc.
    DateTimeOffset CreatedAt);

public record PaginationMetadata(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);
