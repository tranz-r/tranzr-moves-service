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

namespace TranzrMoves.Application.Features.Quote.Patch.CustomerInfo;

public class PatchCustomerInfoStepCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IQuoteProgressCalculator progressCalculator,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IQuoteStepInvalidationService quoteStepInvalidationService,
    IQuoteJourneyProvider quoteJourneyProvider,
    IClock clock,
    ILogger<PatchCustomerInfoStepCommandHandler> logger)
    : ICommandHandler<PatchCustomerInfoStepCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchCustomerInfoStepCommand command,
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

        if (quote.OriginToDestinationDistanceInMiles is null)
            throw new InvalidOperationException("Distance must be calculated before pricing.");

        if (quote.InventoryItems.Count == 0)
            throw new InvalidOperationException("Inventory items are required before pricing.");

        var mapper = new QuoteMapper();

        try
        {
            UpdateCustomerInformation(quote, command);
        }
        catch (Exception e)
        {
            return Error.Failure(e.Message);
        }

        PricingHelper.RecalculateStepState(quote!, QuoteSteps.CustomerInfo, QuoteStepKeys.CustomerInfo,
            quoteStepInvalidationService, progressCalculator,
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

    private void UpdateCustomerInformation(QuoteV2 quote, PatchCustomerInfoStepCommand customerInfo)
    {
        var customer = quote.Customer;
        customer!.FirstName = customerInfo.FirstName;
        customer.LastName = customerInfo.LastName;

        if (customerInfo.IsBillingAddressSameAsOrigin)
        {
            var originAddress = quote.Addresses.First(x => x is { Kind: QuoteAddressKind.Origin });

            customer.UpsertProfileAddress(AddressType.Billing, new AddressV2
            {
                FullAddress = originAddress.FullAddress,
                Line1 = originAddress.Line1,
                Line2 = originAddress.Line2,
                City = originAddress.City,
                County = originAddress.County,
                PostCode = originAddress.PostCode,
                Country = originAddress.Country
            });
        }
        else
        {
            customer.UpsertProfileAddress(AddressType.Billing, new AddressV2
            {
                FullAddress = customerInfo.Address!.FullAddress,
                Line1 = customerInfo.Address.Line1,
                Line2 = customerInfo.Address.Line2,
                City = customerInfo.Address.City,
                County = customerInfo.Address.County,
                PostCode = customerInfo.Address.PostCode,
                Country = customerInfo.Address.Country
            });
        }
    }
}
