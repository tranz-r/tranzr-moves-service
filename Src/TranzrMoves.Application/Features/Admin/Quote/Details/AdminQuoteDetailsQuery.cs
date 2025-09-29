using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.Admin.Quote.Details;

public record AdminQuoteDetailsQuery(Guid QuoteId) : IQuery<ErrorOr<AdminQuoteDetailsResponse>>;

public record AdminQuoteDetailsResponse(AdminQuoteDetailsDto Quote);

public record AdminQuoteDetailsDto(
    Guid Id,
    string QuoteReference,
    string Type,
    string Status,
    decimal TotalCost,
    decimal BaseCost,
    decimal DepositAmount,
    string? ReceiptUrl,
    decimal AdditionalPaymentsTotal,
    string? PaymentStatus,
    string? PaymentMethod,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    string CreatedBy,
    string ModifiedBy,
    AdminCustomerDto? Customer,
    AdminDriverDto? Driver,
    AdminAddressDto? Origin,
    AdminAddressDto? Destination,
    List<AdminInventoryItemDto> InventoryItems,
    List<AdminAdditionalPaymentDto> AdditionalPayments,
    List<AdminPaymentHistoryDto> PaymentHistory,
    AdminServiceDetailsDto ServiceDetails,
    List<AdminNoteDto> AdminNotes);

public record AdminCustomerDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    AdminAddressDto? BillingAddress);

public record AdminDriverDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    string Availability,
    DateTimeOffset? AssignedAt,
    string? VehicleInfo);

public record AdminAddressDto(
    Guid Id,
    string Line1,
    string? Line2,
    string? City,
    string? County,
    string PostCode,
    string? Country,
    string? FullAddress,
    bool? HasElevator,
    int? Floor,
    // Extended Mapbox fields
    string? AddressNumber,
    string? Street,
    string? Neighborhood,
    string? District,
    string? Region,
    string? RegionCode,
    string? CountryCode,
    string? PlaceName,
    string? Accuracy,
    string? MapboxId,
    AdminCoordinatesDto? Coordinates);

public record AdminCoordinatesDto(
    decimal Latitude,
    decimal Longitude);

public record AdminInventoryItemDto(
    Guid Id,
    string Name,
    string? Description,
    int Quantity,
    decimal? Weight,
    AdminDimensionsDto? Dimensions,
    bool Fragile,
    bool RequiresDismantling,
    bool RequiresAssembly);

public record AdminDimensionsDto(
    decimal Length,
    decimal Width,
    decimal Height);

public record AdminAdditionalPaymentDto(
    Guid Id,
    decimal Amount,
    string? Description,
    string? PaymentMethodId,
    string? PaymentIntentId,
    string? ReceiptUrl,
    DateTimeOffset CreatedAt,
    string Status);

public record AdminPaymentHistoryDto(
    Guid Id,
    decimal Amount,
    string Status,
    string? PaymentMethod,
    string? PaymentIntentId,
    string? ReceiptUrl,
    DateTimeOffset ProcessedAt,
    string? FailureReason);

public record AdminServiceDetailsDto(
    string VanType,
    int DriverCount,
    decimal? Hours,
    DateTimeOffset? CollectionDate,
    DateTimeOffset? DeliveryDate,
    bool FlexibleTime,
    string? TimeSlot,
    decimal? DistanceMiles,
    string? PricingTier);

public record AdminNoteDto(
    Guid Id,
    string Note,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    bool IsInternal,
    string? Category);


