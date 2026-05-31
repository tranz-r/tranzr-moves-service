namespace TranzrMoves.Domain.Constants;

public static class PayLaterChargeKeys
{
    public const string Prefix = "paylater:charge:";

    public static string ForQuote(Guid quoteId) => $"{Prefix}{quoteId:N}";
}
