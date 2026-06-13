using Mediator;
using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Application.Features.Quote.Patch.MarketingPreferences;

public sealed record PatchQuoteMarketingPreferencesCommand : ICommand<ErrorOr<MarketingPreferenceDto>>
{
    public Guid QuoteId { get; init; }

    public uint ExpectedVersion { get; init; }

    public bool EmailMarketingEnabled { get; init; }

    public bool SmsMarketingEnabled { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}
