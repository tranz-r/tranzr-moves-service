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

namespace TranzrMoves.Application.Features.Quote.Patch.Addresses;

public class PatchAddressesCommandHandler(
    IQuoteRepository quoteRepository,
    IMapBoxService mapBoxService,
    IQuoteResumeResolver resumeResolver,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IQuoteStepInvalidationService quoteStepInvalidationService,
    IQuoteJourneyProvider quoteJourneyProvider,
    IClock clock,
    IQuoteProgressCalculator progressCalculator,
    ILogger<PatchAddressesCommandHandler> logger)
    : ICommandHandler<PatchAddressesCommand, ErrorOr<QuoteJourneyResponse>>
{
    private const string BaseAddress = "26 Sandyhill lane, Northampton NN3 7AW, United Kingdom";

    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchAddressesCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching collection and delivery addresses");
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
        UpsertQuoteAddresses(quote, command.Addresses, mapper);

        MapRouteV2 quoteOriginToDestinationRoute;
        MapRouteV2 baseToOriginRoute;

        try
        {
            var originAddress = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
            var destinationAddress = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

            if (originAddress is null || destinationAddress is null)
            {
                return Error.Validation("Quote.Addresses.Required", "Origin and destination addresses are required.");
            }

            var originAddressText = BuildAddressText(originAddress.FullAddress, originAddress.Line1, originAddress.PostCode);
            var destinationAddressText = BuildAddressText(destinationAddress.FullAddress, destinationAddress.Line1, destinationAddress.PostCode);

            quoteOriginToDestinationRoute = await mapBoxService.GetRouteDataV2Async(originAddressText, destinationAddressText, cancellationToken);
            baseToOriginRoute = await mapBoxService.GetRouteDataV2Async(BaseAddress, originAddressText, cancellationToken);
        }
        catch (Exception e)
        {
            return Error.NotFound(e.Message);
        }

        UpdateQuoteDistance(quote!, quoteOriginToDestinationRoute, baseToOriginRoute);

        PricingHelper.RecalculateStepState(quote!, QuoteSteps.CollectionDeliveryAddresses,
            QuoteStepKeys.CollectionDeliveryAddresses, quoteStepInvalidationService, progressCalculator,
            quoteJourneyProvider);

        var saveResult = await quoteRepository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }

        var quoteSnapShot = mapper.ToQuoteSnapshotDto(quote);

        var (standardTexts, premiumTexts, additionalServices)
            = await PricingHelper.GetAdditionalServicesAndServiceTextsAsync(clock,
                removalPricingRepository,
                additionalPriceRepository,
                cancellationToken);

        quoteSnapShot.StandardServiceTexts = standardTexts;
        quoteSnapShot.PremiumServiceTexts = premiumTexts;
        quoteSnapShot.AdditionalServices = additionalServices;

        return new QuoteJourneyResponse
        {
            Journey = resumeResolver.Resolve(quote),
            Quote = quoteSnapShot
        };
    }

    private void UpdateQuoteDistance(QuoteV2 quote, MapRouteV2 quoteOriginToDestinationRoute, MapRouteV2 baseToQuoteOriginRoute)
    {
        var quoteOriginToDestinationDistanceInMiles = quoteOriginToDestinationRoute.DistanceMiles;
        var baseToQuoteOriginDistanceInMiles = baseToQuoteOriginRoute.DistanceMiles;

        quote.OriginDestinationRoute = quoteOriginToDestinationRoute;
        quote.OriginToDestinationDistanceInMiles = quoteOriginToDestinationDistanceInMiles;
        quote.BaseToOriginDistanceInMiles = baseToQuoteOriginDistanceInMiles;
    }

    private static void UpsertQuoteAddresses(QuoteV2 quote, IReadOnlyCollection<QuoteAddressDto> incoming, QuoteMapper mapper)
    {
        // Keep one address per kind and map DTO -> entity through QuoteMapper.
        var dedupedByKind = incoming
            .GroupBy(x => x.Kind)
            .Select(x => x.Last())
            .ToArray();

        var mappedAddresses = dedupedByKind
            .Select(mapper.ToQuoteAddress)
            .ToList();

        foreach (var mapped in mappedAddresses)
        {
            mapped.QuoteId = quote.Id;
        }

        quote.Addresses.Clear();
        quote.Addresses.AddRange(mappedAddresses);
    }

    private static string BuildAddressText(string? fullAddress, string line1, string postCode) =>
        !string.IsNullOrWhiteSpace(fullAddress)
            ? fullAddress
            : $"{line1}, {postCode}";
}
