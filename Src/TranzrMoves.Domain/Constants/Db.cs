namespace TranzrMoves.Domain.Constants;

public static class Db
{
    public const string CONNECTION_STRING_NAME = "TranzrMovesDatabaseConnection";

    public static class Tables
    {
        public const string Users = nameof(Users);
        public const string Quotes = nameof(Quotes);
        public const string QuoteSessions = nameof(QuoteSessions);
        public const string CustomerQuotes = nameof(CustomerQuotes);
        public const string InventoryItems = nameof(InventoryItems);
        public const string DriverQuotes = nameof(DriverQuotes);
        public const string RateCards = nameof(RateCards);
        public const string ServiceFeatures = nameof(ServiceFeatures);
        public const string AdditionalPrices = nameof(AdditionalPrices);
    }
}