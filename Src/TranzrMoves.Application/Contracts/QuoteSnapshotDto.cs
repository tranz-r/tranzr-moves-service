// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public sealed class QuoteSnapshotDto
{
    public Guid Id { get; init; }

    /// <summary>
    /// PostgreSQL xmin row version for optimistic concurrency. Send as <c>If-Match</c> on the next PATCH.
    /// </summary>
    public uint Version { get; init; }

    public string QuoteReference { get; init; } = string.Empty;
    public QuoteType Type { get; set; } // Send, Receive, Removals
    public long? OriginToDestinationDistanceInMiles { get; set; }
    public IReadOnlyList<QuoteAddressDto> Addresses { get; init; } = [];
    public long? BaseToOriginDistanceInMiles { get; set; }
    public MapRouteV2Dto? OriginDestinationRoute { get; set; }
    public int NumberOfItemsToDismantle { get; init; }
    public int NumberOfItemsToAssemble { get; init; }
    public VanType VanType { get; set; }
    public int RecommendedVanCount { get; set; }
    public int SelectedVanCount { get; set; }
    public decimal TotalInventoryVolumeM3 { get; set; }
    public VanCapacityStatus? VanCapacityStatus { get; set; }
    public string? VanCapacityWarning { get; set; }
    public long CrewCount { get; init; }
    public ScheduleV2Dto? Schedule { get; init; }
    public IReadOnlyList<InventoryItemDto> InventoryItems { get; init; } = [];
    public IReadOnlyList<PricingOptionDto> Pricings { get; init; } = [];
    public QuoteCustomerDto? Customer { get; init; }
    public IReadOnlyList<ServiceTextDto> StandardServiceTexts { get; set; } = [];
    public IReadOnlyList<ServiceTextDto> PremiumServiceTexts { get; set; } = [];
    public PricingTier? PricingTier { get; set; }
    public decimal? QuotePrice { get; init; }
    public decimal? TotalCost { get; init; }
    public ExtraPricesDto? AdditionalServices { get; set; }
}

public sealed class QuoteCustomerDto
{
    public Guid Id { get; init; }
    public string? FullName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public AddressV2Dto? BillingAddress { get; init; }
}

public sealed class AddressV2Dto
{
    public Guid Id { get; init; }
    public AddressType Type { get; init; }
    public Guid? UserId { get; init; }
    public string? FullAddress { get; init; }
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string? City { get; init; }
    public string? County { get; init; }
    public string PostCode { get; init; } = string.Empty;
    public string? Country { get; init; }
    public bool? HasElevator { get; init; }
    public int? Floor { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed class PricingOptionDto
{
    public Guid Id { get; init; }
    public Guid QuoteId { get; init; }
    public QuoteType QuoteType { get; init; }
    public ServiceLevel ServiceLevel { get; init; }
    public int SelectedVanCount { get; init; }
    public int CrewCount { get; init; }
    public decimal BaseVanCost { get; init; }
    public decimal DistanceCost { get; init; }
    public decimal LabourCost { get; init; }
    public decimal BaseToOriginCost { get; init; }
    public int NumberOfItemsToDismantle { get; init; }
    public int NumberOfItemsToAssemble { get; init; }
    public decimal DismantleCost { get; init; }
    public decimal AssemblyCost { get; init; }
    public decimal OriginFloorSurcharge { get; init; }
    public decimal DestinationFloorSurcharge { get; init; }
    public decimal SpecialHandlingCost { get; init; }
    public decimal CallOutCharge { get; init; }
    public decimal UlezSurcharge { get; init; }
    public decimal CongestionZoneSurcharge { get; init; }
    public decimal InsuranceUplift { get; init; }
    public bool CongestionChargeApplies { get; init; }
    public int StandardBlockHours { get; init; }
    public decimal CostPerHourAfterStandard { get; init; }
    public int Vat { get; init; }
    public decimal VatRate { get; init; }
    public decimal SubtotalWithoutVat { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalCostWithVat { get; init; }
    public bool IsSelected { get; init; }
}

public sealed class QuoteAddressDto
{
    public Guid Id { get; init; }
    public QuoteAddressKind Kind { get; set; }
    public string? FullAddress { get; init; }
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string? City { get; init; }
    public string? County { get; init; }
    public string PostCode { get; init; } = string.Empty;
    public string? Country { get; init; }
    public bool? HasElevator { get; init; }
    public int? Floor { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed class ScheduleV2Dto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Instant? CollectionDate { get; set; }
    public Instant? DeliveryDate { get; set; }
    public int? Hours { get; set; }
    public bool? FlexibleTime { get; set; }
    public TimeSlot? TimeSlot { get; set; } // morning, afternoon, evening
}
