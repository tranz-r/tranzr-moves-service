namespace TranzrMoves.Domain.Constants;

public static class DepositBalanceChargeKeys
{
    public const string Prefix = "deposit:charge:";

    public static string ForQuote(Guid quoteId) => $"{Prefix}{quoteId:N}";
}
