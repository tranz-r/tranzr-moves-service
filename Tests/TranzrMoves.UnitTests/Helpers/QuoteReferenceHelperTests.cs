using FluentAssertions;
using NodaTime;
using NSubstitute;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Helpers;

namespace TranzrMoves.UnitTests.Helpers;

public class QuoteReferenceHelperTests
{
    private const string ReferencePattern = @"^TRZ-\d{6}-[0-9A-F]{3}$";

    [Fact]
    public void GenerateQuoteReference_FromLocalDate_FormatsDateAndHexSuffix()
    {
        var reference = QuoteReferenceHelper.GenerateQuoteReference(new LocalDate(2026, 4, 5));

        reference.Should().MatchRegex(ReferencePattern);
        reference.Should().StartWith("TRZ-260405-");
        reference.Should().HaveLength(14); // TRZ- + yyMMdd + - + 3
    }

    [Fact]
    public void GenerateQuoteReference_FromTimeService_UsesUtcToday()
    {
        var time = Substitute.For<ITimeService>();
        time.TodayInUtc().Returns(new LocalDate(2026, 1, 15));

        var reference = QuoteReferenceHelper.GenerateQuoteReference(time);

        reference.Should().StartWith("TRZ-260115-");
        reference.Should().MatchRegex(ReferencePattern);
    }

    [Fact]
    public void GenerateQuoteReference_FromLocalDate_ProducesDistinctValues_WhenSampleIsSmall()
    {
        var date = new LocalDate(2026, 4, 5);
        var set = new HashSet<string>();
        // Only 4,096 possible suffixes per day; keep n small to avoid flaky birthday collisions in CI.
        for (var i = 0; i < 8; i++)
            set.Add(QuoteReferenceHelper.GenerateQuoteReference(date));

        set.Should().HaveCount(8);
    }
}
