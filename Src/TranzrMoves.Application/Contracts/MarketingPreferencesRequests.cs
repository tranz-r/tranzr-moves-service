namespace TranzrMoves.Application.Contracts;

public sealed class UpdateMarketingPreferencesRequest
{
    public bool EmailMarketingEnabled { get; set; }

    public bool SmsMarketingEnabled { get; set; }
}

public sealed class MarketingPreferencesResponse
{
    public bool EmailMarketingEnabled { get; set; }

    public bool SmsMarketingEnabled { get; set; }
}

public sealed class PatchQuoteMarketingPreferencesRequest
{
    public bool EmailMarketingEnabled { get; set; }

    public bool SmsMarketingEnabled { get; set; }
}
