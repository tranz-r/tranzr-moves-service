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
    Paid,
    PartiallyPaid,
    PaymentSetup,
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
    Later,
    Balance
}

public enum TimeSlot
{
    morning = 1,
    afternoon,
    evening
}

public enum ServiceLevel
{
    Standard = 0,
    Premium  = 1
}

public enum AdditionalPriceType
{
    Dismantle = 1,
    Assembly = 2,
    Storage = 3
}

public enum PaymentMetadata
{
    PaymentType = 1,
    TotalCost,
    DepositPercentage,
    DepositAmount,
    BalanceAmount,
    DueDate,
    QuoteReference,
    PaymentMethodId,
    PaymentDueDate,
    QuoteId,
    CustomerEmail,
    PaymentAmount
}

public enum LegalDocumentType
{
    TermsAndConditions = 1,
    PrivacyPolicy = 2
}