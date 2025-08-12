namespace TranzrMoves.Domain.Constants;

public static class Db
{
    public const string CONNECTION_STRING_NAME = "TranzrMovesDatabaseConnection";

    public static class Tables
    {
        public const string Users = nameof(Users);
        public const string Jobs = nameof(Jobs);
        public const string CustomerJobs = nameof(CustomerJobs);
        public const string InventoryItems = nameof(InventoryItems);
        public const string DriverJobs = nameof(DriverJobs);
    }
}