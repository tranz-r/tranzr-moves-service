using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common;
using TranzrMoves.Application.Common.Strategy;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.Patch.Inventory;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Patch.EmailAndPhoneNumber;

public class PatchCustomerEmailAndPhoneCommandHandler(
    IQuoteRepository quoteRepository,
    IUserV2Repository userV2Repository,
    IQuoteResumeResolver resumeResolver,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IClock clock,
    ILogger<PatchInventoryCommandHandler> logger)
    : ICommandHandler<PatchCustomerEmailAndPhoneCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchCustomerEmailAndPhoneCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching customer email and phone number");
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

        // If customer with this email exists, update it, otherwise create a new one.
        var user = await userV2Repository.GetUserByEmailAsync(command.Email, cancellationToken);

        if (user is not null)
        {
            user.PhoneNumber = command.PhoneNumber;
            var updatedUser = await userV2Repository.UpdateUserAsync(user, cancellationToken);

            if (updatedUser.IsError)
            {
                return updatedUser.Errors;
            }

            quote.CustomerId = updatedUser.Value.Id;
        }
        else
        {
            var newUser = await userV2Repository.AddUserAsync(
                new UserV2 { Email = command.Email, PhoneNumber = command.PhoneNumber }, cancellationToken);

            if (newUser.IsError)
            {
                return newUser.Errors;
            }

            quote.CustomerId = newUser.Value.Id;
        }

        // Lead capture only.
        // This should not affect the main quote journey navigation.
        quote.StepsDirty &= ~QuoteSteps.CustomerEmailAndPhoneNumber;
        quote.StepsCompleted |= QuoteSteps.CustomerEmailAndPhoneNumber;

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

        var mapper = new QuoteMapper();
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
