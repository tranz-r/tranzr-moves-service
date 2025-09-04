using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class QuoteMapper
{
    // ========== ENTITY ➜ DTO ==========
    // Flatten entity fields into nested DTOs
    [MapProperty(nameof(Quote.InventoryItems), nameof(QuoteDto.Items))]
    [MapProperty(nameof(Quote.CollectionDate), nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DateISO))]
    [MapProperty(nameof(Quote.DeliveryDate),  nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DeliveryDateISO))]
    [MapProperty(nameof(Quote.Hours),         nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.Hours))]
    [MapProperty(nameof(Quote.FlexibleTime),  nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.FlexibleTime))]
    [MapProperty(nameof(Quote.TimeSlot),      nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.TimeSlot))]
    [MapProperty(nameof(Quote.PricingTier),   nameof(QuoteDto.Pricing)  + "." + nameof(PricingDto.PricingTier))]
    [MapProperty(nameof(Quote.TotalCost),     nameof(QuoteDto.Pricing)  + "." + nameof(PricingDto.TotalCost))]
    [MapProperty(nameof(Quote.PaymentStatus), nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.Status))]
    [MapProperty(nameof(Quote.PaymentType),   nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.PaymentType))]
    [MapProperty(nameof(Quote.DepositAmount), nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.DepositAmount))]
    [MapProperty(nameof(Quote.PaymentIntentId), nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.PaymentIntentId))]
    [MapProperty(nameof(Quote.PaymentMethodId), nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.PaymentMethodId))]
    [MapProperty(nameof(Quote.ReceiptUrl), nameof(QuoteDto.Payment)  + "." + nameof(PaymentDto.ReceiptUrl))]
    // Note: Customer properties are not part of Quote entity
    public partial QuoteDto ToDto(Quote? src);
    
    public partial List<QuoteDto> ToDtoList(List<Quote> src);

    // ========== DTO ➜ ENTITY (create new) ==========
    // Only specify the asymmetric pieces; everything else maps by name.
    [MapperIgnoreTarget(nameof(Quote.Id))]      // Id should not be changed
    [MapProperty(nameof(QuoteDto.Items), nameof(Quote.InventoryItems))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DateISO),         nameof(Quote.CollectionDate))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DeliveryDateISO), nameof(Quote.DeliveryDate))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.Hours),           nameof(Quote.Hours))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.FlexibleTime),    nameof(Quote.FlexibleTime))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.TimeSlot),        nameof(Quote.TimeSlot))]
    [MapProperty(nameof(QuoteDto.Pricing) + "." + nameof(PricingDto.PricingTier),       nameof(Quote.PricingTier))]
    [MapProperty(nameof(QuoteDto.Pricing) + "." + nameof(PricingDto.TotalCost),         nameof(Quote.TotalCost))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.Status),            nameof(Quote.PaymentStatus))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentType),       nameof(Quote.PaymentType))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.DepositAmount),     nameof(Quote.DepositAmount))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentMethodId),     nameof(Quote.PaymentMethodId))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentIntentId),     nameof(Quote.PaymentIntentId))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.ReceiptUrl),     nameof(Quote.ReceiptUrl))]
    // Note: Customer properties are not part of Quote entity
    public partial Quote ToEntity(QuoteDto src);
    
    public partial List<Quote> ToEntityList(List<QuoteDto> src);

    // ========== DTO ➜ ENTITY (update existing) ==========
    // Use this for PATCH/PUT so EF keys/audit/navigation stay intact.
    [MapperIgnoreTarget(nameof(Quote.Version))] // Version is a concurrency token, managed by EF
    [MapperIgnoreTarget(nameof(Quote.Id))]      // Id should not be changed
    [MapperIgnoreTarget(nameof(Quote.CreatedAt))] // CreatedAt should not be changed
    [MapperIgnoreTarget(nameof(Quote.ModifiedAt))] // ModifiedAt is managed by EF
    [MapperIgnoreTarget(nameof(Quote.CustomerQuotes))] // Navigation properties not handled here
    [MapperIgnoreTarget(nameof(Quote.DriverQuotes))]   // Navigation properties not handled here
    // Only specify the asymmetric pieces; everything else maps by name.
    [MapProperty(nameof(QuoteDto.Items), nameof(Quote.InventoryItems))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DateISO),         nameof(Quote.CollectionDate))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.DeliveryDateISO), nameof(Quote.DeliveryDate))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.Hours),           nameof(Quote.Hours))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.FlexibleTime),    nameof(Quote.FlexibleTime))]
    [MapProperty(nameof(QuoteDto.Schedule) + "." + nameof(ScheduleDto.TimeSlot),        nameof(Quote.TimeSlot))]
    [MapProperty(nameof(QuoteDto.Pricing) + "." + nameof(PricingDto.PricingTier),       nameof(Quote.PricingTier))]
    [MapProperty(nameof(QuoteDto.Pricing) + "." + nameof(PricingDto.TotalCost),         nameof(Quote.TotalCost))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.Status),            nameof(Quote.PaymentStatus))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentType),       nameof(Quote.PaymentType))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.DepositAmount),     nameof(Quote.DepositAmount))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentMethodId),     nameof(Quote.PaymentMethodId))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.PaymentIntentId),     nameof(Quote.PaymentIntentId))]
    [MapProperty(nameof(QuoteDto.Payment) + "." + nameof(PaymentDto.ReceiptUrl),     nameof(Quote.ReceiptUrl))]
    public partial void UpdateEntity(QuoteDto src, Quote target);

    // ========== Nested types ==========
    // Mapperly will use these for the collections and nested objects.
    public partial AddressDto ToAddressDto(Address src);
    public partial Address ToAddress(AddressDto src);

    public partial InventoryItemDto ToInventoryItemDto(InventoryItem src);
    public partial InventoryItem ToInventoryItem(InventoryItemDto src);

    // ========== Small converters for nullability/asymmetry ==========
    // Entity has PaymentStatus?; DTO has non-null Status.
    private PaymentStatus MapPaymentStatus(PaymentStatus? s) => s ?? PaymentStatus.Pending;

    // DTO has VanType?; Entity requires VanType (non-null).
    private VanType MapVanType(VanType? v) => v ?? default; // choose your desired default

    // (Optional) if you want the reverse conversions explicit:
    private PaymentStatus? MapPaymentStatusNullable(PaymentStatus s) => s;
    private VanType? MapVanTypeNullable(VanType v) => v;
}