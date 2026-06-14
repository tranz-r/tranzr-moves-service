using FluentAssertions;
using NodaTime;
using TranzrMoves.Application.Services;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.UnitTests.Application;

public sealed class QuoteResumeResolverTests
{
    [Fact]
    public void Resolve_AfterExtrasPatch_UsesNextJourneyStepEvenWhenCustomerInfoDataIsComplete()
    {
        var resolver = CreateResolver();
        var quote = BuildRemovalsQuoteWithCustomerInfoDataComplete();
        quote.LastCompletedStepKey = QuoteStepKeys.Extras;

        var journey = resolver.Resolve(quote);

        journey.ResumeStepKey.Should().Be(QuoteStepKeys.CustomerInfo);
        journey.ResumeUrl.Should().Be("/origin-destination");
        journey.Steps.Single(x => x.Key == QuoteStepKeys.CustomerInfo).Status.Should().Be("current");
    }

    [Fact]
    public void Resolve_WithoutLastCompletedStep_FallsBackToFirstIncompleteStep()
    {
        var resolver = CreateResolver();
        var quote = new QuoteV2
        {
            Id = Guid.NewGuid(),
            Type = QuoteType.Removals,
            LastCompletedStepKey = null
        };

        var journey = resolver.Resolve(quote);

        journey.ResumeStepKey.Should().Be(QuoteStepKeys.CollectionDeliveryAddresses);
        journey.ResumeUrl.Should().Be("/collection-delivery");
    }

    private static QuoteResumeResolver CreateResolver()
    {
        IQuoteJourneyProvider journeyProvider = new QuoteJourneyProvider();
        IQuoteProgressCalculator progressCalculator = new QuoteProgressCalculator(journeyProvider);
        IClock clock = SystemClock.Instance;

        return new QuoteResumeResolver(journeyProvider, progressCalculator, clock);
    }

    private static QuoteV2 BuildRemovalsQuoteWithCustomerInfoDataComplete()
    {
        var customer = new UserV2
        {
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "07123456789",
            Email = "jane@example.com"
        };
        customer.UpsertProfileAddress(AddressType.Billing, new AddressV2
        {
            Line1 = "1 Billing Street",
            PostCode = "SW1A 1AA"
        });

        return new QuoteV2
        {
            Id = Guid.NewGuid(),
            Type = QuoteType.Removals,
            OptionalExtas = true,
            ServiceTier = ServiceLevel.Standard,
            TotalCost = 100m,
            PriceCalculatedAt = SystemClock.Instance.GetCurrentInstant(),
            Customer = customer,
            Addresses =
            [
                new QuoteAddress
                {
                    Kind = QuoteAddressKind.Origin,
                    Line1 = "1 Origin Street",
                    PostCode = "E1 1AA",
                    Latitude = 51.5,
                    Longitude = -0.1,
                    Floor = 0,
                    HasElevator = false
                },
                new QuoteAddress
                {
                    Kind = QuoteAddressKind.Destination,
                    Line1 = "2 Destination Street",
                    PostCode = "W1 1AA",
                    Latitude = 51.5,
                    Longitude = -0.2,
                    Floor = 1,
                    HasElevator = true
                }
            ],
            InventoryItems = [new QuoteInventoryItem { Name = "Sofa" }],
            Schedule = new Schedule
            {
                CollectionDate = SystemClock.Instance.GetCurrentInstant(),
                FlexibleTime = true
            },
            StepsCompleted = QuoteSteps.Extras
        };
    }
}
