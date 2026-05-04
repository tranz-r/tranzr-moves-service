namespace TranzrMoves.Domain.Constants;

public static class Db
{
    public const string CONNECTION_STRING_NAME = "TranzrMovesDatabaseConnection";
    public const string SCHEMA = "tranzrmoves";

    public static class Sequences
    {
        public const string QuoteReference = "quote_reference_seq";
    }

    public static class Tables
    {
        public const string Users = nameof(Users);
        public const string UsersV2 = nameof(UsersV2);
        public const string Quotes = nameof(Quotes);
        public const string QuotesV2 = nameof(QuotesV2);
        public const string AddressesV2 = nameof(AddressesV2);
        public const string QuoteAddresses = nameof(QuoteAddresses);
        public const string Schedules = nameof(Schedules);
        public const string Pricings = nameof(Pricings);
        public const string Payments = nameof(Payments);
        public const string QuoteSessions = nameof(QuoteSessions);
        public const string CustomerQuotes = nameof(CustomerQuotes);
        public const string InventoryItems = nameof(InventoryItems);
        public const string InventoryItemsV2 = nameof(InventoryItemsV2);
        public const string DriverQuotes = nameof(DriverQuotes);
        public const string RateCards = nameof(RateCards);
        public const string ServiceFeatures = nameof(ServiceFeatures);
        public const string DataProtectionKeys = nameof(DataProtectionKeys);
        public const string AdditionalPrices = nameof(AdditionalPrices);
        public const string LegalDocuments = nameof(LegalDocuments);
        public const string QuoteAdditionalPayments = nameof(QuoteAdditionalPayments);
        public const string InventoryCategories = nameof(InventoryCategories);
        public const string InventoryGoods = nameof(InventoryGoods);
    }
}
