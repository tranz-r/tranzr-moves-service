// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Statics;

public static class QuoteCompletionRules
{
    public static bool HasCompletedCustomerInfo(QuoteV2 quote)
    {
        var customer = quote.Customer;

        var res = customer is not null
               && !string.IsNullOrWhiteSpace(customer.FirstName)
               && !string.IsNullOrWhiteSpace(customer.LastName)
               && !string.IsNullOrWhiteSpace(customer.PhoneNumber)
               && HasBillingAddress(customer);

        return res;
    }

    private static bool HasBillingAddress(UserV2 customer)
    {
        return customer.BillingAddress is not null
               || customer.Addresses.Any(x => x.Type == AddressType.Billing);
    }

    public static bool HasCompletedCustomerEmailAndPhoneNumber(QuoteV2 quote)
    {
        return quote.Customer?.Email is not null
               && quote.Customer.PhoneNumber is not null;
    }

    public static bool HasCompletedAddressesForSend(QuoteV2 quote)
    {
        var collection = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
        var delivery = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

        return IsValidAddress(collection)
               && IsValidAddress(delivery)
               && HasAccessDetails(collection)
               && HasAccessDetails(delivery);
    }

    public static bool HasCompletedAddressesForReceive(QuoteV2 quote)
    {
        var collection = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
        var delivery = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

        return IsValidAddress(collection)
               && IsValidAddress(delivery)
               && HasAccessDetails(collection)
               && HasAccessDetails(delivery);
    }

    public static bool HasCompletedAddressesForRemovals(QuoteV2 quote)
    {
        var pickupAddress = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
        var deliveryAddress = quote.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

        return IsValidAddress(deliveryAddress)
               && IsValidAddress(pickupAddress)
               && HasAccessDetails(deliveryAddress)
               && HasAccessDetails(pickupAddress);
    }

    public static bool HasCompletedInventory(QuoteV2 quote)
    {
        var res = quote.InventoryItems.Count > 0;
        return res;
    }

    public static bool HasCompletedSchedule(QuoteV2 quote)
    {
        if (quote.Schedule is null)
            return false;

        return quote.Schedule.CollectionDate is not null
               && (
                    quote.Schedule.FlexibleTime == true ||
                    quote.Schedule.TimeSlot is not null
                  );
    }

    public static bool HasCompletedPricing(QuoteV2 quote) =>
        quote.ServiceTier is not null
        && quote.TotalCost is > 0
        && quote.PriceCalculatedAt is not null;

    /// <summary>
    /// All quote data required to show and validate the summary step, before the customer confirms it.
    /// </summary>
    public static bool HasQuoteSummaryPreflightComplete(QuoteV2 quote) =>
        HasCompletedAddresses(quote)
        && HasCompletedInventory(quote)
        && HasCompletedSchedule(quote)
        && HasCompletedCustomerEmailAndPhoneNumber(quote)
        && HasCompletedPricing(quote)
        && HasCompletedCustomerInfo(quote);

    /// <summary>Preflight complete and the customer has called <c>PATCH .../quote-summary</c> at least once.</summary>
    public static bool HasCompletedQuoteSummary(QuoteV2 quote) =>
        HasQuoteSummaryPreflightComplete(quote) && quote.SummaryConfirmedAt is not null;

    public static bool HasCompletedAddresses(QuoteV2 quote) =>
        quote.Type switch
        {
            QuoteType.Send => HasCompletedAddressesForSend(quote),
            QuoteType.Receive => HasCompletedAddressesForReceive(quote),
            QuoteType.Removals => HasCompletedAddressesForRemovals(quote),
            _ => false
        };

    private static bool IsValidAddress(QuoteAddress? address) =>
        address is not null
        && !string.IsNullOrWhiteSpace(address.Line1)
        && !string.IsNullOrWhiteSpace(address.PostCode)
        && address.Latitude is not null
        && address.Longitude is not null;

    private static bool HasAccessDetails(QuoteAddress? address) =>
        address is not null
        && address.Floor is not null
        && address.HasElevator is not null;

    public static bool HasCompletedPayment(QuoteV2 quote) =>
        HasCompletedQuoteSummary(quote)
        && quote.Payments != null
        && quote.PaymentStatus is not null
        && quote.Payments.Any(x => x.CustomerSelectedOption);
}
