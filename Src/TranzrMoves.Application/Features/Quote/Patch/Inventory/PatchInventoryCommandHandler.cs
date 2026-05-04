using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common;
using TranzrMoves.Application.Common.Strategy;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Patch.Inventory;

public class PatchInventoryCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IQuoteProgressCalculator progressCalculator,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IQuoteStepInvalidationService quoteStepInvalidationService,
    IQuoteJourneyProvider quoteJourneyProvider,
    IClock clock,
    ILogger<PatchInventoryCommandHandler> logger)
    : ICommandHandler<PatchInventoryCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchInventoryCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching move date and time");
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);

        if (quote == null)
        {
            logger.LogInformation("No quote found for {QuoteId}", command.QuoteId);
            return Error.NotFound($"No quote found for id {command.QuoteId}");
        }

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        var mapper = new QuoteMapper();

        var inventoryItems = command.InventoryItems.Select(mapper.ToQuoteInventoryItem);

        quote.InventoryItems.Clear();
        quote.InventoryItems.AddRange(inventoryItems);

        var totalVolume = PricingHelper.CalculateTotalVolume(quote);

        var quoteRecommendedVanCount = Math.Max(
            1,
            (int)Math.Ceiling(totalVolume / PricingHelper.EffectiveVanCapacityM3));

        quote.RecommendedVanCount = quoteRecommendedVanCount;
        quote.TotalInventoryVolumeM3 = PricingHelper.Round(totalVolume);
        quote.EffectiveVanCapacityM3 = PricingHelper.Round(PricingHelper.EffectiveVanCapacityM3);

        PricingHelper.RecalculateStepState(quote!, QuoteSteps.Inventory,
            QuoteStepKeys.Inventory, quoteStepInvalidationService, progressCalculator,
            quoteJourneyProvider);

        var saveResult = await quoteRepository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }

        var (standardTexts, premiumTexts, additionalServices)
            = await PricingHelper.GetAdditionalServicesAndServiceTextsAsync(clock,
                removalPricingRepository,
                additionalPriceRepository,
                cancellationToken);

        var quoteSnapShot = mapper.ToQuoteSnapshotDto(quote);
        quoteSnapShot.StandardServiceTexts = standardTexts;
        quoteSnapShot.PremiumServiceTexts = premiumTexts;
        quoteSnapShot.AdditionalServices = additionalServices;

        return new QuoteJourneyResponse
        {
            Journey = resumeResolver.Resolve(quote),
            Quote = quoteSnapShot
        };
    }
}
