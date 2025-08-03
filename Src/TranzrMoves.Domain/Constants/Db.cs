namespace TranzrMoves.Domain.Constants;

public static class Db
{
    public const string CONNECTION_STRING_NAME = "TranzrMovesDatabaseConnection";

    public static class Tables
    {
        public const string Users = nameof(Users);
        public const string Jobs = nameof(Jobs);
        public const string UserJobs = nameof(UserJobs);
        public const string Addresses = nameof(Addresses);
        public const string InventoryItems = nameof(InventoryItems);
        public const string PricingTiers = nameof(PricingTiers);
    }
}