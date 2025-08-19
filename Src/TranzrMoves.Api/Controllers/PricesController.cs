using System.Text.Json.Serialization;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class PricesController(IMediator mediator) : ApiControllerBase
{
    [HttpPost]
    public IActionResult CreateJobAsync([FromBody] QuoteRequest req)
    {
        // Debug logging to check what we receive
        Console.WriteLine("=== DEBUG: CreateJobAsync called ===");
        Console.WriteLine($"Request is null: {req == null}");
        
        if (req == null)
        {
            Console.WriteLine("ERROR: Request object is null - JSON deserialization failed");
            return BadRequest("Invalid request - could not deserialize JSON payload");
        }
        
        Console.WriteLine($"DistanceMiles: {req.DistanceMiles}");
        Console.WriteLine($"Items is null: {req.Items == null}");
        Console.WriteLine($"Items count: {req.Items?.Count ?? -1}");
        
        if (req.Items != null)
        {
            for (int i = 0; i < req.Items.Count; i++)
            {
                var item = req.Items[i];
                Console.WriteLine($"Item {i}: {(item == null ? "NULL" : $"Id={item.Id}, Name={item.Name}, LengthCm={item.LengthCm}")}");
            }
        }
        Console.WriteLine("=====================================");
        
        var cfg = new PricingConfig();

        // Validate request
        if (req.Items == null || req.Items.Count == 0)
        {
            return BadRequest("No items provided in the request");
        }

        // 1) Per-item analysis → totals + breakdown (shared for both tiers)
        double totalVol = 0;
        int bulkyCount = 0;
        int veryBulkyCount = 0;
        var itemsBreakdown = new List<ItemBreakdown>();

        foreach (var item in req.Items)
        {
            if (item.Quantity <= 0) continue;

            var unitVolM3 = item.VolumeM3;
            var longestEdgeM = item.LongestEdgeCm / 100.0;
            var secondEdgeM = item.SecondLongestEdgeCm / 100.0;

            var category = ClassifyBulkiness(unitVolM3, longestEdgeM, secondEdgeM, cfg, out var reasons);

            if (category == BulkinessCategory.Bulky) bulkyCount += item.Quantity;
            if (category == BulkinessCategory.VeryBulky) veryBulkyCount += item.Quantity;

            var lineVol = unitVolM3 * item.Quantity;
            totalVol += lineVol;

            itemsBreakdown.Add(new ItemBreakdown
            {
                Id = item.Id,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitLengthCm = item.LengthCm,
                UnitWidthCm = item.WidthCm,
                UnitHeightCm = item.HeightCm,
                UnitLongestEdgeM = longestEdgeM,
                UnitSecondEdgeM = secondEdgeM,
                UnitVolumeM3 = unitVolM3,
                LineVolumeM3 = lineVol,
                Bulkiness = category,
                BulkinessReasons = reasons.ToArray()
            });
        }

        // 2) Distance base (shared)
        decimal basePrice = BaseByDistance(req.DistanceMiles, cfg.DistanceBands, cfg.PerMileOver500);

        // 3) Volume surcharge (shared)
        decimal volFee = VolumeFee(totalVol, cfg.VolumeSurcharge);

        // 4) Crew rules → recommended movers (shared)
        var (recommendedMovers, crewReasons) = RecommendMovers(totalVol, bulkyCount, veryBulkyCount, req, cfg);

        // If customer overrides movers
        int chosenMovers = Math.Max(recommendedMovers,
            req.RequestedMovers > 0 ? req.RequestedMovers : recommendedMovers);

        // Crew fee = each mover beyond driver (shared)
        int extraMovers = Math.Max(0, chosenMovers - 1);

        decimal crewFee = default;
        if (extraMovers == 1)
        {
            crewFee = extraMovers * cfg.ExtraMoverFlatFee;
        }

        if (extraMovers == 2)
        {
            crewFee = extraMovers * cfg.ExtraTwoMoversFlatFee;
        }

        // Access & extras (shared)
        decimal stairsFee = bulkyCount * req.StairsFloors * cfg.StairsPerBulkyPerFloor;
        decimal longCarryFee = (req.LongCarry ? 1 : 0) * bulkyCount * cfg.LongCarryPerBulky;
        decimal assemblyFee = req.NumberOfItemsToAssemble * cfg.AssemblyFeePerItem;
        decimal dismantleFee = req.NumberOfItemsToDismantle * cfg.DismantleFeePerItem;
        decimal extrasFee = req.ParkingFee + req.UlezFee;

        // Calculate for both tiers
        var standardBreakdown = CalculateTierBreakdown(TierType.standard, basePrice, volFee, crewFee, 
            stairsFee, longCarryFee, assemblyFee, dismantleFee, extrasFee, recommendedMovers, 
            chosenMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, itemsBreakdown, 
            req.VatRegistered, cfg);

        // Premium tier includes assembly/dismantle fees and 2 movers
        // Calculate premium crew fee (only charge for 3rd+ movers)
        decimal premiumCrewFee = CalculatePremiumCrewFee(chosenMovers, cfg);
        var premiumBreakdown = CalculateTierBreakdown(TierType.premium, basePrice, volFee, premiumCrewFee, 
            stairsFee, longCarryFee, assemblyFee, dismantleFee, extrasFee, recommendedMovers, 
            chosenMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, itemsBreakdown, 
            req.VatRegistered, cfg);

        var result = new PickUpDropOffPrice
        {
            Standard = standardBreakdown,
            Premium = premiumBreakdown
        };

        return Ok(result);
    }

    // ---------- Helpers ----------
    private static QuoteBreakdown CalculateTierBreakdown(
        TierType tier, decimal basePrice, decimal volFee, decimal crewFee,
        decimal stairsFee, decimal longCarryFee, decimal assemblyFee, decimal dismantleFee, decimal extrasFee,
        int recommendedMovers, int chosenMovers, List<string> crewReasons,
        double totalVol, int bulkyCount, int veryBulkyCount, List<ItemBreakdown> itemsBreakdown,
        bool vatRegistered, PricingConfig cfg)
    {
        // Premium tier uplift - calculated to ensure Premium is roughly double Standard
        decimal uplift = tier switch
        {
            TierType.premium => CalculatePremiumUplift(basePrice, volFee, crewFee, assemblyFee, dismantleFee),
            _ => 0m
        };

        decimal tieredSubtotal = basePrice + uplift + volFee;

        // Guardrail & VAT
        decimal totalExVat = Math.Max(basePrice,
            tieredSubtotal + crewFee + stairsFee + longCarryFee + assemblyFee + dismantleFee + extrasFee);
        decimal customerTotal = vatRegistered
            ? Math.Round(totalExVat * (1 + cfg.VatRate), 0)
            : Math.Round(totalExVat, 0);

        return new QuoteBreakdown
        {
            // pricing
            Base = basePrice,
            VolumeSurcharge = volFee,
            TierUplift = uplift,
            TieredSubtotal = tieredSubtotal,

            // crew
            RecommendedMinimumMovers = recommendedMovers,
            RequestedMovers = chosenMovers,
            CrewFee = crewFee,
            CrewRuleReasons = crewReasons.ToArray(),

            // access & extras
            StairsFee = stairsFee,
            LongCarryFee = longCarryFee,
            AssemblyFee = assemblyFee,
            DismantleFee = dismantleFee,
            ExtrasFee = extrasFee,

            // totals
            TotalExVat = totalExVat,
            VatRate = cfg.VatRate,
            CustomerTotal = customerTotal,

            // context
            TotalVolumeM3 = totalVol,
            BulkyItemsCount = bulkyCount,
            VeryBulkyItemsCount = veryBulkyCount,

            // per-item
            Items = itemsBreakdown.ToArray()
        };
    }

    private static BulkinessCategory ClassifyBulkiness(
        double volumeM3, double longestEdgeM, double secondEdgeM, PricingConfig cfg, out List<string> reasons)
    {
        reasons = new List<string>();

        if (volumeM3 >= cfg.VeryBulkyVolumeM3)
        {
            reasons.Add($"Volume ≥ {cfg.VeryBulkyVolumeM3:0.##} m³");
            return BulkinessCategory.VeryBulky;
        }

        if (volumeM3 >= cfg.BulkyVolumeM3)
        {
            reasons.Add($"Volume ≥ {cfg.BulkyVolumeM3:0.##} m³");
            return BulkinessCategory.Bulky;
        }

        if (longestEdgeM >= cfg.BulkyLongestEdgeM && secondEdgeM >= 0.6)
        {
            reasons.Add($"Edges ≥ {cfg.BulkyLongestEdgeM:0.##} m & ≥ 0.6 m");
            return BulkinessCategory.Bulky;
        }

        return BulkinessCategory.Normal;
    }

    private static (int movers, List<string> reasons) RecommendMovers(
        double totalVol, int bulkyCount, int veryBulkyCount, QuoteRequest req, PricingConfig cfg)
    {
        var reasons = new List<string>();
        int movers = 1;

        // Simple rule: Recommend 2 movers if any of these conditions are met
        // This covers the majority of moves safely and efficiently
        
        if (veryBulkyCount > 0)
        {
            movers = 2;
            reasons.Add("Very bulky items present (≥1.2 m³) - 2 movers recommended for safety");
        }
        else if (req.StairsFloors >= 2 && bulkyCount > 0)
        {
            movers = 2;
            reasons.Add("Multiple floors with bulky items - 2 movers recommended for stairs");
        }
        else if (req.LongCarry && bulkyCount > 0)
        {
            movers = 2;
            reasons.Add("Long carry with bulky items - 2 movers recommended for distance");
        }
        else if (totalVol >= cfg.ForceHelperAtTotalVolumeM3)
        {
            movers = 2;
            reasons.Add($"High volume move (≥{cfg.ForceHelperAtTotalVolumeM3} m³) - 2 movers recommended");
        }
        else if (bulkyCount >= 3)
        {
            movers = 2;
            reasons.Add("Multiple bulky items - 2 movers recommended for efficiency");
        }

        // Note: Customers can optionally add a 3rd mover if they want extra help
        // The system will charge appropriately for the additional mover
        
        return (movers, reasons);
    }

    private static decimal CalculatePremiumCrewFee(int chosenMovers, PricingConfig cfg)
    {
        // Premium tier includes 2 movers, only charge for additional movers beyond 2
        int premiumExtraMovers = Math.Max(0, chosenMovers - 2);
        
        if (premiumExtraMovers == 0) return 0m;
        if (premiumExtraMovers == 1) return premiumExtraMovers * cfg.ExtraMoverFlatFee;
        
        // For 2+ extra movers (i.e., 4+ total movers), use the two-mover rate
        return premiumExtraMovers * cfg.ExtraTwoMoversFlatFee;
    }

    private static decimal CalculatePremiumUplift(decimal basePrice, decimal volFee, decimal standardCrewFee, 
        decimal assemblyFee, decimal dismantleFee)
    {
        // Calculate what Standard tier total would be (base service components)
        decimal standardBaseService = basePrice + volFee + standardCrewFee;
        
        // Premium uplift strategy: 
        // 1. Double the base service cost
        // 2. Add value of included assembly/dismantle services
        // 3. Add premium service enhancement
        
        decimal doubleBaseService = standardBaseService * 1.00m; // 100% uplift for doubling
        decimal includedServicesValue = (assemblyFee + dismantleFee) * 0.8m; // 80% value of included services
        decimal premiumEnhancement = (basePrice + volFee) * 0.25m; // 25% enhancement for premium quality
        
        return doubleBaseService + includedServicesValue + premiumEnhancement;
    }

    private static decimal BaseByDistance(double miles, List<DistanceBand> bands, decimal perMileOverLast)
    {
        var ordered = bands.OrderBy(b => b.MaxMiles).ToList();
        foreach (var b in ordered)
            if (miles <= b.MaxMiles)
                return b.BasePrice;

        var last = ordered.Last();
        return last.BasePrice + (decimal)(miles - last.MaxMiles) * perMileOverLast;
    }

    private static decimal VolumeFee(double totalM3, List<VolumeBand> bands)
    {
        foreach (var b in bands.OrderBy(b => b.MaxM3))
            if (totalM3 <= b.MaxM3)
                return b.Fee;

        return 0;
    }
}

// -------------------- Models --------------------
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
    public int NumberOfItemsToAssemble { get; init; } = 0;
    public int NumberOfItemsToDismantle { get; init; } = 0;
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

public class ItemBreakdown
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }

    public double UnitLengthCm { get; set; }
    public double UnitWidthCm { get; set; }
    public double UnitHeightCm { get; set; }
    public double UnitLongestEdgeM { get; set; }
    public double UnitSecondEdgeM { get; set; }

    public double UnitVolumeM3 { get; set; }
    public double LineVolumeM3 { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BulkinessCategory Bulkiness { get; set; }

    public string[] BulkinessReasons { get; set; } = Array.Empty<string>();
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

    // Bulkiness thresholds
    public double BulkyVolumeM3 { get; init; } = 0.50;
    public double BulkyLongestEdgeM { get; init; } = 1.60;
    public double VeryBulkyVolumeM3 { get; init; } = 1.20;

    // Crew enforcement thresholds
    public double ForceHelperAtTotalVolumeM3 { get; init; } = 8.0;
}

// -------------------- Response --------------------
public class PickUpDropOffPrice
{
    public QuoteBreakdown Standard { get; set; } = new();
    public QuoteBreakdown Premium { get; set; } = new();
}

public class QuoteBreakdown
{
    public decimal Base { get; set; }
    public decimal VolumeSurcharge { get; set; }
    public decimal TierUplift { get; set; }
    public decimal TieredSubtotal { get; set; }

    // crew
    public int RecommendedMinimumMovers { get; set; }
    public int RequestedMovers { get; set; }
    public decimal CrewFee { get; set; }
    public string[] CrewRuleReasons { get; set; } = Array.Empty<string>();

    // access & extras
    public decimal StairsFee { get; set; }
    public decimal LongCarryFee { get; set; }
    public decimal AssemblyFee { get; set; }
    public decimal DismantleFee { get; set; }
    public decimal ExtrasFee { get; set; }

    // totals
    public decimal TotalExVat { get; set; }
    public decimal VatRate { get; set; }
    public decimal CustomerTotal { get; set; }

    // context
    public double TotalVolumeM3 { get; set; }
    public int BulkyItemsCount { get; set; }
    public int VeryBulkyItemsCount { get; set; }

    public ItemBreakdown[] Items { get; set; } = Array.Empty<ItemBreakdown>();
}