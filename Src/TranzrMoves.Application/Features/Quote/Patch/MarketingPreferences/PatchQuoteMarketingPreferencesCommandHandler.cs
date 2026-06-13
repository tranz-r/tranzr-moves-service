using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Application.Features.Quote.Patch.MarketingPreferences;

public sealed class PatchQuoteMarketingPreferencesCommandHandler(
    IQuoteRepository quoteRepository,
    IMarketingPreferenceService marketingPreferenceService,
    ILogger<PatchQuoteMarketingPreferencesCommandHandler> logger)
    : ICommandHandler<PatchQuoteMarketingPreferencesCommand, ErrorOr<MarketingPreferenceDto>>
{
    public async ValueTask<ErrorOr<MarketingPreferenceDto>> Handle(
        PatchQuoteMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching marketing preferences for quote {QuoteId}", command.QuoteId);

        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, asTracking: true);
        if (quote is null)
        {
            return Error.NotFound($"No quote found for id {command.QuoteId}");
        }

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        var email = quote.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation(
                "Quote.MarketingPreferences.EmailRequired",
                "Customer email must be saved before marketing preferences can be updated.");
        }

        var preferences = await marketingPreferenceService.ApplyPreferencesAsync(
            new ApplyMarketingPreferencesRequest(
                email,
                command.EmailMarketingEnabled,
                command.SmsMarketingEnabled,
                MarketingConsentSource.QuoteJourney,
                quote.CustomerId,
                command.IpAddress,
                command.UserAgent),
            cancellationToken);

        return preferences;
    }
}
