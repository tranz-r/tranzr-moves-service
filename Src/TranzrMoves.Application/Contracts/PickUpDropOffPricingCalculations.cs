// -------------------- Models --------------------

using System.Text.Json.Serialization;

namespace TranzrMoves.Application.Contracts;

public class QuoteItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double LengthCm { get; set; }
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore]
    public double VolumeM3 =>
        (LengthCm / 100.0) * (WidthCm / 100.0) * (HeightCm / 100.0);

    [JsonIgnore]
    public double LongestEdgeCm =>
        Math.Max(LengthCm, Math.Max(WidthCm, HeightCm));

    [JsonIgnore]
    public double SecondLongestEdgeCm
    {
        get
        {
            var dims = new[] { LengthCm, WidthCm, HeightCm };
            Array.Sort(dims);
            return dims[1];
        }
    }
}

public class QuoteRequest
{
    public double DistanceMiles { get; set; }

    public List<QuoteItem> Items { get; set; } = [];

    public int StairsFloors { get; set; } = 0;
    public bool LongCarry { get; set; } = false;
    public decimal ParkingFee { get; set; } = 0;
    public decimal UlezFee { get; set; } = 0;
    public bool VatRegistered { get; set; } = true;

    // New: customer can override crew size
    public int RequestedMovers { get; set; } = 0;
}

public record DistanceBand(int MaxMiles, decimal BasePrice);

public record VolumeBand(double MaxM3, decimal Fee);

public record Tier(TierType Name, decimal Multiplier);

public enum TierType
{
    standard = 1,
    premium
}

public enum BulkinessCategory
{
    Normal,
    Bulky,
    VeryBulky
}

// -------------------- Config --------------------
public class PricingConfig
{
    public List<DistanceBand> DistanceBands { get; init; } = new()
    {
        new(5, 10),
        new(10, 20),
        new(15, 40),
        new(20, 60),
        new(25, 80),
        new(50, 100),
        new(75, 125),   // ✅ Sweet spot preserved
        new(100, 150),
        new(125, 180),
        new(150, 210),
        new(175, 240),
        new(200, 270),
        new(225, 300),
        new(250, 330),
        new(275, 360),
        new(300, 390),
        new(350, 450),
        new(400, 510),
        new(450, 570),
        new(500, 630),
    };

    public decimal PerMileOver500 { get; init; } = 1.50m;

    public List<VolumeBand> VolumeSurcharge { get; init; } = new()
    {
        new(1.0, 0), 
        new(3.0, 10), 
        new(5.0, 20),
        new(8.0, 35), 
        new(12.0, 60), 
        new(18.0, 90),
    };

    public Dictionary<TierType, Tier> Tiers { get; init; } = new()
    {
        [TierType.standard] = new(TierType.standard, 1.00m),
        [TierType.premium] = new(TierType.premium, 1.00m),
    };

    public decimal StairsPerBulkyPerFloor { get; init; } = 10;
    public decimal LongCarryPerBulky { get; init; } = 10;
    public decimal AssemblyFeePerItem { get; init; } = 25;
    public decimal DismantleFeePerItem { get; init; } = 18;
    public decimal VatRate { get; init; } = 0.20m;

    // New crew fee
    public decimal ExtraMoverFlatFee { get; init; } = 25m;
    public decimal ExtraTwoMoversFlatFee { get; init; } = 45m;
    public decimal ExtraThreeMoversFlatFee { get; init; } = 60m;

    // Bulkiness thresholds
    public double BulkyVolumeM3 { get; init; } = 0.50;
    public double BulkyLongestEdgeM { get; init; } = 1.60;
    public double VeryBulkyVolumeM3 { get; init; } = 1.20;

    // Crew enforcement thresholds
    public double ForceHelperAtTotalVolumeM3 { get; init; } = 8.0;
}

// -------------------- Response --------------------

public class QuoteBreakdown
{
    public decimal Base { get; set; }
    public decimal VolumeSurcharge { get; set; }
    public decimal TierUplift { get; set; }
    public decimal TieredSubtotal { get; set; }

    // crew
    public int RecommendedMinimumMovers { get; set; }
    public string[] CrewRuleReasons { get; set; } = [];

    // access & extras
    public decimal StairsFee { get; set; }
    public decimal LongCarryFee { get; set; }
    public decimal ExtrasFee { get; set; }

    // totals
    public decimal TotalExVat { get; set; }
    public decimal CustomerTotal { get; set; }

    // context
    public double TotalVolumeM3 { get; set; }
    public int BulkyItemsCount { get; set; }
    public int VeryBulkyItemsCount { get; set; }
}