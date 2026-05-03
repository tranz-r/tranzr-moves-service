namespace TranzrMoves.Application.Contracts;

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/collection-delivery-addresses</c>.
/// Concurrency: <c>If-Match</c> header (quote row version).
/// </summary>
public sealed class PatchAddressesRequest
{
    public List<QuoteAddressDto> Addresses { get; set; } = [];
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/inventory</c>.
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchInventoryRequest
{
    public List<InventoryItemDto> InventoryItems { get; set; } = [];
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/move-date-time</c>.
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchMoveDateTimeStepRequest
{
    public ScheduleV2Dto Schedule { get; set; } = default!;

    public int SelectedVanCount { get; set; }
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/customer-info</c>.
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchCustomerInfoStepRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsBillingAddressSameAsOrigin { get; set; }

    public QuoteAddressDto? Address { get; set; }
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/pricing</c>.
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchPricingStepRequest
{
    public Guid PricingId { get; set; }

    public int NumberOfSelectedVans { get; set; }

    public int NumberOfItemsToDismantle { get; set; }

    public int NumberOfItemsToAssemble { get; set; }
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/quote-summary</c> (typically <c>{}</c>).
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchQuoteSummaryStepRequest
{
}

/// <summary>
/// JSON body for <c>PATCH /api/v2/Quote/{quoteId}/customer-email-phone</c>.
/// Concurrency: <c>If-Match</c> header.
/// </summary>
public sealed class PatchCustomerEmailPhoneRequest
{
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
