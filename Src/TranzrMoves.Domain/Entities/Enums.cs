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

[Flags]
public enum QuoteSteps
{
    None = 0,
    CollectionDeliveryAddresses = 1,
    Inventory = 2,
    MoveDateAndTimeSlot = 4,
    CustomerEmailAndPhoneNumber = 8,
    Pricing = 16,
    RemovalPricing = 32,
    Extras = 64,
    CustomerInfo = 128,
    QuoteSummary = 256,
    Payment = 512,
    Complete = 1024,
}


public enum StripePaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Cancelled = 3
}

public enum PricingTier
{
    eco = 1,
    ecoPlus,
    standard,
    premium
}

public enum ServiceTier
{
    Standard,
    Premium
}

public enum VanCapacityStatus
{
    Recommended = 1,
    Warning = 2,
    Blocked = 3
}


public enum AddressType
{
    Residential = 0,
    Commercial = 1,
    Billing
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
    Balance,
    Adhoc
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
    Premium = 1
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
    PaymentAmount,
    ExtraCharges,
    ExtraChargesDescription
}

public enum LegalDocumentType
{
    TermsAndConditions = 1,
    PrivacyPolicy = 2
}

public enum QuoteType
{
    Send,
    Receive,
    Removals
}

public enum QuoteAddressKind
{
    Origin = 1,
    Destination,
    Base
}

public enum BusinessAccountStatus
{
    Active = 0,
    Suspended,
    Closed
}

public enum BusinessUserStatus
{
    Active = 0,
    Invited,
    Suspended,
    Deactivated
}

public enum BusinessUserRole
{
    Owner = 0,
    Admin,
    Finance,
    Member,
    Viewer
}
