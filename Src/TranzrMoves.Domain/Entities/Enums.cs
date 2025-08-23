namespace TranzrMoves.Domain.Entities;

public enum Role
{
    none = 0,
    customer = 1,
    admin,
    driver,
    commercial_client
}

public enum PaymentStatus
{
    Pending = 0,
    Failed,
    Succeeded,
    Cancelled
}

public enum PricingTier
{
    eco = 1,
    ecoPlus,
    standard,
    premium
}

public enum VanType
{
    largeVan = 1,
    SmallVan,
    mediumVan,
    xlLuton
}

public enum PaymentType
{
    Full,
    Deposit,
    Later
}

public enum TimeSlot
{
    morning = 1,
    afternoon,
    evening
}