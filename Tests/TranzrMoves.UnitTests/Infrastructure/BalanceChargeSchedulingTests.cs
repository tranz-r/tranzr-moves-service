using FluentAssertions;
using NodaTime;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.UnitTests.Infrastructure;

public sealed class BalanceChargeSchedulingTests
{
    [Fact]
    public void GetDepositChargeInstant_Returns_0005_London_On_CollectionDate()
    {
        var collectionDate = new LocalDate(2026, 6, 15);
        var instant = BalanceChargeScheduling.GetDepositChargeInstant(collectionDate);

        var london = instant.InZone(DateTimeZoneProviders.Tzdb["Europe/London"]);
        london.Date.Should().Be(collectionDate);
        london.TimeOfDay.Should().Be(new LocalTime(0, 5));
    }

    [Fact]
    public void IsDepositChargeDue_When_Before_0005_On_MoveDay_Returns_False()
    {
        var collectionDate = new LocalDate(2026, 6, 15);
        var zone = DateTimeZoneProviders.Tzdb["Europe/London"];
        var now = collectionDate.At(new LocalTime(0, 4)).InZoneLeniently(zone).ToInstant();

        BalanceChargeScheduling.IsDepositChargeDue(collectionDate, now).Should().BeFalse();
    }

    [Fact]
    public void IsDepositChargeDue_When_At_0005_On_MoveDay_Returns_True()
    {
        var collectionDate = new LocalDate(2026, 6, 15);
        var now = BalanceChargeScheduling.GetDepositChargeInstant(collectionDate);

        BalanceChargeScheduling.IsDepositChargeDue(collectionDate, now).Should().BeTrue();
    }

    [Fact]
    public void IsDepositChargeDue_When_After_MoveDay_Returns_True()
    {
        var collectionDate = new LocalDate(2026, 6, 14);
        var now = BalanceChargeScheduling.GetDepositChargeInstant(new LocalDate(2026, 6, 15));

        BalanceChargeScheduling.IsDepositChargeDue(collectionDate, now).Should().BeTrue();
    }
}
