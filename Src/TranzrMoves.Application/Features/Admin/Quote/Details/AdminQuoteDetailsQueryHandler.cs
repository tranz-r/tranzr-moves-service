using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;

using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.Details;

public class AdminQuoteDetailsQueryHandler(
    IQuoteRepository quoteRepository,
    ILogger<AdminQuoteDetailsQueryHandler> logger) : IQueryHandler<AdminQuoteDetailsQuery, ErrorOr<AdminQuoteDetailsResponse>>
{
    public async ValueTask<ErrorOr<AdminQuoteDetailsResponse>> Handle(AdminQuoteDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving admin quote details for quote {QuoteId}", request.QuoteId);

            var quote = await quoteRepository.GetAdminQuoteDetailsAsync(request.QuoteId, cancellationToken);

            if (quote == null)
            {
                logger.LogWarning("Quote {QuoteId} not found", request.QuoteId);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {request.QuoteId} not found");
            }

            var quoteDto = MapToAdminQuoteDetailsDto(quote);

            logger.LogInformation("Successfully retrieved admin quote details for quote {QuoteId}", request.QuoteId);

            return new AdminQuoteDetailsResponse(quoteDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving admin quote details for quote {QuoteId}", request.QuoteId);
            return Error.Failure("AdminQuoteDetails.Failed", "Failed to retrieve quote details");
        }
    }

    private static AdminQuoteDetailsDto MapToAdminQuoteDetailsDto(Domain.Entities.Quote quote)
    {
        // Calculate total cost including additional payments
        var baseCost = quote.TotalCost ?? 0;
        var additionalPaymentsTotal = quote.QuoteAdditionalPayments?.Sum(ap => ap.Amount) ?? 0;

        decimal totalCost;

        if (quote.PaymentType is PaymentType.Deposit)
        {
            totalCost = quote.DepositAmount!.Value + (baseCost - quote.DepositAmount.Value);
        }
        else
        {
            totalCost = baseCost + additionalPaymentsTotal;
        }

        var depositAmount = quote.DepositAmount ?? 0;
        var receiptUrl = quote?.ReceiptUrl ?? null;

        // Map customer information
        var customerQuote = quote.CustomerQuotes?.FirstOrDefault();
        var customer = customerQuote?.User != null ? new AdminCustomerDto(
            customerQuote.User.Id,
            customerQuote.User.FullName ?? "",
            customerQuote.User.FirstName ?? "",
            customerQuote.User.LastName ?? "",
            customerQuote.User.Email ?? "",
            customerQuote.User.PhoneNumber ?? "",
            customerQuote.User.Role?.ToString() ?? "",
            customerQuote.User.CreatedAt,
            null, // LastLoginAt not available in User entity
            customerQuote.User.BillingAddress != null ? MapToAdminAddressDto(customerQuote.User.BillingAddress) : null) : null;

        // Map driver information
        var driverQuote = quote.DriverQuotes?.FirstOrDefault();
        var driver = driverQuote?.User != null ? new AdminDriverDto(
            driverQuote.User.Id,
            driverQuote.User.FullName ?? "",
            driverQuote.User.FirstName ?? "",
            driverQuote.User.LastName ?? "",
            driverQuote.User.Email ?? "",
            driverQuote.User.PhoneNumber ?? "",
            driverQuote.User.Role?.ToString() ?? "",
            "Available", // TODO: Get actual availability from driver entity
            driverQuote.CreatedAt,
            null) : null; // TODO: Get vehicle info from driver entity

        // Map origin address
        var origin = quote.Origin != null ? MapToAdminAddressDto(quote.Origin) : null;

        // Map destination address
        var destination = quote.Destination != null ? MapToAdminAddressDto(quote.Destination) : null;

        // Map inventory items
        var inventoryItems = quote.InventoryItems?.Select(item => new AdminInventoryItemDto(
            item.Id,
            item.Name,
            item.Description,
            item.Quantity ?? 0,
            null, // Weight not available in InventoryItem entity
            item.Width.HasValue && item.Height.HasValue && item.Depth.HasValue ? new AdminDimensionsDto(
                item.Width.Value,
                item.Height.Value,
                item.Depth.Value) : null,
            false, // Fragile not available in InventoryItem entity
            false, // RequiresDismantling not available in InventoryItem entity
            false)).ToList() ?? new List<AdminInventoryItemDto>(); // RequiresAssembly not available in InventoryItem entity

        // Map additional payments
        var additionalPayments = quote.QuoteAdditionalPayments?.Select(payment => new AdminAdditionalPaymentDto(
            payment.Id,
            payment.Amount,
            payment.Description,
            payment.PaymentMethodId,
            payment.PaymentIntentId,
            payment.ReceiptUrl,
            DateTimeOffset.UtcNow, // CreatedAt not available in QuoteAdditionalPayment entity
            "Completed")).ToList() ?? new List<AdminAdditionalPaymentDto>();

        // Map payment history (placeholder - would need actual payment history entity)
        var paymentHistory = new List<AdminPaymentHistoryDto>();

        // Map service details
        var serviceDetails = new AdminServiceDetailsDto(
            quote.VanType.ToString(),
            (int)quote.DriverCount,
            quote.Hours,
            quote.CollectionDate,
            quote.DeliveryDate,
            quote.FlexibleTime ?? false,
            quote.TimeSlot?.ToString(),
            quote.DistanceMiles,
            quote.PricingTier?.ToString());

        // Map admin notes (placeholder - would need actual admin notes entity)
        var adminNotes = new List<AdminNoteDto>();

        return new AdminQuoteDetailsDto(
            quote.Id,
            quote.QuoteReference,
            quote.Type.ToString(),
            quote.PaymentStatus?.ToString() ?? "Pending",
            totalCost,
            baseCost,
            depositAmount,
            receiptUrl,
            additionalPaymentsTotal,
            quote.PaymentStatus?.ToString(),
            quote.PaymentType.ToString(),
            quote.CreatedAt,
            quote.ModifiedAt,
            quote.CreatedBy,
            quote.ModifiedBy,
            customer,
            driver,
            origin,
            destination,
            inventoryItems,
            additionalPayments,
            paymentHistory,
            serviceDetails,
            adminNotes);
    }

    private static AdminAddressDto MapToAdminAddressDto(Domain.Entities.Address address)
    {
        return new AdminAddressDto(
            address.Id,
            address.Line1,
            address.Line2,
            address.City,
            address.County,
            address.PostCode,
            address.Country,
            address.FullAddress,
            address.HasElevator,
            address.Floor,
            // Extended Mapbox fields
            address.AddressNumber,
            address.Street,
            address.Neighborhood,
            address.District,
            address.Region,
            address.RegionCode,
            address.CountryCode,
            address.PlaceName,
            address.Accuracy,
            address.MapboxId,
            address.Latitude.HasValue && address.Longitude.HasValue ? new AdminCoordinatesDto(
                (decimal)address.Latitude.Value,
                (decimal)address.Longitude.Value) : null);
    }
}
