using System.Text.Json.Serialization;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

// public class PaymentSheetRequest
// {
//     public required string Email { get; set; }
//     public required string Name { get; set; }
//     public required decimal Amount { get; set; }
// }

public class JobAddressBase
{
    public string Line1 { get; set; }
    public string Postcode { get; set; }
}

public class Cost
{
    public int BaseVan { get; set; }
    public double Distance { get; set; }
    public int Floors { get; set; }
    public int ElevatorAdjustment { get; set; }
    public int Drivers { get; set; }
    public double TierAdjustment { get; set; }
    public double Total { get; set; }
}

public class Customer
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public JobAddressBase BillingAddress { get; set; }
}

public class JobAddress : JobAddressBase
{
    public int Floor { get; set; }
    public bool HasElevator { get; set; }
}

public class PaymentSheetRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VanType Van { get; set; }
    public int DriverCount { get; set; }
    public double DistanceMiles { get; set; }
    public JobAddress Origin { get; set; }
    public JobAddress Destination { get; set; }
    public string PricingTier { get; set; }
    public DateTime CollectionDate { get; set; }
    public Customer Customer { get; set; }
    // public string Email { get; set; }
    // public string Name { get; set; }
    // public double Amount { get; set; }
    public Cost Cost { get; set; }
}