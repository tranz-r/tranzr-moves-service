using System.Collections.ObjectModel;

using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class QuoteV2 : IAuditable
{
    public Guid Id { get; set; }

    public string SessionId { get; set; } = string.Empty;
    public QuoteType Type { get; set; }

    public long? OriginToDestinationDistanceInMiles { get; set; }
    public long? BaseToOriginDistanceInMiles { get; set; }
    public string? OriginDestinationRoute { get; set; }

    public int NumberOfItemsToDismantle { get; set; } = 0;
    public int NumberOfItemsToAssemble { get; set; } = 0;

    public VanType VanType { get; set; }
    public long CrewCount { get; set; }
    public string QuoteReference { get; set; } = string.Empty;

    // Quote-level van recommendation
    public decimal? TotalInventoryVolumeM3 { get; set; }
    public decimal? EffectiveVanCapacityM3 { get; set; }
    public int? RecommendedVanCount { get; set; }
    public int? SelectedVanCount { get; set; }
    public VanCapacityStatus? VanCapacityStatus { get; set; }
    public string? VanCapacityWarning { get; set; }
    public Instant? VanRecommendationCalculatedAt { get; set; }

    public Schedule? Schedule { get; set; }

    public Guid? CustomerId { get; set; }

    // Final selected pricing outcome
    public ServiceLevel? ServiceTier { get; set; }
    public decimal? QuotePrice { get; set; }
    public decimal? TotalCost { get; set; }
    public Instant? PriceCalculatedAt { get; set; }

    public QuoteSteps StepsCompleted { get; set; }
    public QuoteSteps StepsDirty { get; set; } = QuoteSteps.None;
    public string? LastCompletedStepKey { get; set; }

    /// <summary>Set when the customer explicitly confirms the quote summary step (<c>PATCH .../quote-summary</c>).</summary>
    public Instant? SummaryConfirmedAt { get; set; }

    public PaymentStatus? PaymentStatus { get; set; }
    public uint Version { get; set; }

    public Instant? LastResumeEmailSentAt { get; set; }
    public Instant? ExpiresAt { get; set; }

    public UserV2? Customer { get; set; }
    public Collection<Payment>? Payments { get; set; } = [];

    // Prefer one collection eventually
    public List<Pricing> Pricings { get; set; } = [];

    public List<QuoteAddress> Addresses { get; set; } = [];
    public List<QuoteInventoryItem> InventoryItems { get; set; } = [];

    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}

public sealed class Pricing : IAuditable
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }

    public QuoteType QuoteType { get; set; }
    public ServiceLevel ServiceLevel { get; set; }

    public int SelectedVanCount { get; set; }
    public int CrewCount { get; set; }

    public decimal BaseVanCost { get; set; }
    public decimal DistanceCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal BaseToOriginCost { get; set; }
    public int NumberOfItemsToDismantle { get; set; }
    public int NumberOfItemsToAssemble { get; set; }
    public decimal DismantleCost { get; set; }
    public decimal AssemblyCost { get; set; }
    public decimal OriginFloorSurcharge { get; set; }
    public decimal DestinationFloorSurcharge { get; set; }
    public decimal SpecialHandlingCost { get; set; }
    public decimal CallOutCharge { get; set; }
    public decimal UlezSurcharge { get; set; }
    public decimal CongestionZoneSurcharge { get; set; }
    public decimal InsuranceUplift { get; set; }
    public bool CongestionChargeApplies { get; set; }
    public int StandardBlockHours { get; set; }
    public decimal CostPerHourAfterStandard { get; set; }
    public int Vat { get; set; } = 20;
    public decimal VatRate { get; set; }
    public decimal SubtotalWithoutVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalCostWithVat { get; set; }
    public bool IsSelected { get; set; }
    public QuoteV2? Quote { get; set; }
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}

public sealed class Schedule : IAuditable
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Instant? CollectionDate { get; set; }
    public Instant? DeliveryDate { get; set; }
    public int? Hours { get; set; }
    public bool? FlexibleTime { get; set; }
    public TimeSlot? TimeSlot { get; set; } // morning, afternoon, evening
    public QuoteV2? Quote { get; set; }
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}

public sealed class Payment : IAuditable
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public StripePaymentStatus Status { get; set; } = StripePaymentStatus.Pending;
    public string? PaymentMethodId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? SetupIntentId { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal? Amount { get; set; }
    public decimal? RemainingAmount { get; set; } = 0;
    public string? ReceiptUrl { get; set; }
    public LocalDate? DueDate { get; set; } // When full payment is due
    public string? StripeSessionId { get; set; }
    public bool CustomerSelectedOption { get; set; } = false;
    public QuoteV2? Quote { get; set; }
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}
