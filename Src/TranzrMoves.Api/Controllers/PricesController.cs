using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Prices.Removals;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class PricesController(IMediator mediator) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RemovalPricingDto>> GetPickUpDropOffPricingAsync([FromBody] QuoteRequest req, CancellationToken cancellationToken)
    {
        var cfg = new PricingConfig();

        // 1) Per-item analysis → totals + breakdown (shared for both tiers)
        double totalVol = 0;
        int bulkyCount = 0;
        int veryBulkyCount = 0;
        //var itemsBreakdown = new List<ItemBreakdown>();

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
        }

        // 2) Distance base (shared)
        decimal basePrice = BaseByDistance(req.DistanceMiles, cfg.DistanceBands, cfg.PerMileOver500);

        // 3) Volume surcharge (shared)
        decimal volFee = VolumeFee(totalVol, cfg.VolumeSurcharge);

        // 4) Crew rules → recommended movers (shared)
        var (recommendedMovers, crewReasons) = RecommendMovers(totalVol, bulkyCount, veryBulkyCount, req, cfg);

        // Access & extras (shared)
        decimal stairsFee = bulkyCount * req.StairsFloors * cfg.StairsPerBulkyPerFloor;
        decimal longCarryFee = (req.LongCarry ? 1 : 0) * bulkyCount * cfg.LongCarryPerBulky;

        decimal extrasFee = req.ParkingFee + req.UlezFee;
        
        // 1 Mover price calculation for standard and premium
        var oneMoverStandard = CalculateTierBreakdown(TierType.standard, basePrice, volFee, 1 * cfg.ExtraMoverFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        var oneMoverPremium = CalculateTierBreakdown(TierType.premium, basePrice, volFee, 1 * cfg.ExtraMoverFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        // 2 Movers price calculation for standard and premium
        var twoMoversStandard = CalculateTierBreakdown(TierType.standard, basePrice, volFee, 1 * cfg.ExtraTwoMoversFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        var twoMoversPremium = CalculateTierBreakdown(TierType.premium, basePrice, volFee, 1 * cfg.ExtraTwoMoversFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        // 3 Movers price calculation for standard and premium
        var threeMoversStandard = CalculateTierBreakdown(TierType.standard, basePrice, volFee, 1 * cfg.ExtraThreeMoversFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        var threeMoversPremium = CalculateTierBreakdown(TierType.premium, basePrice, volFee, 1 * cfg.ExtraThreeMoversFlatFee, 
            stairsFee, longCarryFee, extrasFee, recommendedMovers, crewReasons, totalVol, bulkyCount, veryBulkyCount, 
            req.VatRegistered, cfg);
        
        var basePriceCalculationResult = await mediator.Send(new RemovalPricesRequest(DateTimeOffset.UtcNow), cancellationToken);


        var basePriceCalculation = basePriceCalculationResult.Value;
        
        basePriceCalculation!.Rates.One!.Standard!.PickUpDropOff = oneMoverStandard;
        basePriceCalculation!.Rates.One!.Premium!.PickUpDropOff = oneMoverPremium;
        
        basePriceCalculation!.Rates.Two!.Standard!.PickUpDropOff = twoMoversStandard;
        basePriceCalculation!.Rates.Two!.Premium!.PickUpDropOff = twoMoversPremium;
        
        basePriceCalculation!.Rates.Three!.Standard!.PickUpDropOff = threeMoversStandard;
        basePriceCalculation!.Rates.Three!.Premium!.PickUpDropOff = threeMoversPremium;

        return Ok(basePriceCalculation);
    }
    
    [HttpGet("removal-prices")]
    public async Task<ActionResult<RemovalPricingDto>> GetRemovalPricesAsync(CancellationToken ct)
    {
        var response = await mediator.Send(new RemovalPricesRequest(DateTimeOffset.UtcNow), ct);
        
        var removalPricing = response.Value;
        
        var etag = removalPricing.Version;
        if (Request.Headers.TryGetValue(HeaderNames.IfMatch, out var inm) && inm.ToString() == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=21600"; // tune as needed
        return Ok(removalPricing);
    }

    // ---------- Helpers ----------
    private static QuoteBreakdown CalculateTierBreakdown(
        TierType tier, decimal basePrice, decimal volFee, decimal crewFee,
        decimal stairsFee, decimal longCarryFee, decimal extrasFee,
        int recommendedMovers, List<string> crewReasons,
        double totalVol, int bulkyCount, int veryBulkyCount,
        bool vatRegistered, PricingConfig cfg)
    {
        // Premium tier uplift - calculated to ensure Premium is roughly double Standard
        decimal uplift = tier switch
        {
            TierType.premium => CalculatePremiumUplift(basePrice, volFee, crewFee),
            _ => 0m
        };

        decimal tieredSubtotal = basePrice + uplift + volFee;

        // Guardrail & VAT
        decimal totalExVat = Math.Max(basePrice,
            tieredSubtotal + crewFee + stairsFee + longCarryFee + extrasFee);
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
            //CrewFee = crewFee,
            CrewRuleReasons = crewReasons.ToArray(),

            // access & extras
            StairsFee = stairsFee,
            LongCarryFee = longCarryFee,
            ExtrasFee = extrasFee,

            // totals
            TotalExVat = totalExVat,
            CustomerTotal = customerTotal,

            // context
            TotalVolumeM3 = totalVol,
            BulkyItemsCount = bulkyCount,
            VeryBulkyItemsCount = veryBulkyCount
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

    private static decimal CalculatePremiumUplift(decimal basePrice, decimal volFee, decimal standardCrewFee)
    {
        // Calculate what Standard tier total would be (base service components)
        decimal standardBaseService = basePrice + volFee + standardCrewFee;
        
        // Premium uplift strategy: 
        // 1. Double the base service cost
        // 2. Add value of included assembly/dismantle services
        // 3. Add premium service enhancement
        
        decimal doubleBaseService = standardBaseService * 1.00m; // 100% uplift for doubling
        // decimal includedServicesValue = (assemblyFee + dismantleFee) * 0.8m; // 80% value of included services
        decimal premiumEnhancement = (basePrice + volFee) * 0.25m; // 25% enhancement for premium quality
        
        return doubleBaseService + premiumEnhancement;
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