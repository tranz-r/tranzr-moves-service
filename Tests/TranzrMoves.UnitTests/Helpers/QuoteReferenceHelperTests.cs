using FluentAssertions;
using NodaTime;
using TranzrMoves.Application.Helpers;

namespace TranzrMoves.UnitTests.Helpers;

public class QuoteReferenceHelperTests
{
    [Fact]
    public void FormatQuoteReference_FormatsDateAndUnpaddedSequence()
    {
        var reference = QuoteReferenceHelper.FormatQuoteReference(new LocalDate(2026, 4, 5), 42);

        reference.Should().Be("TRZ-260405-42");
    }

    [Fact]
    public void FormatQuoteReference_FirstValueIsOne()
    {
        var reference = QuoteReferenceHelper.FormatQuoteReference(new LocalDate(2026, 4, 5), 1);

        reference.Should().Be("TRZ-260405-1");
    }

    [Fact]
    public void FormatQuoteReference_SupportsLargeSequence()
    {
        var reference = QuoteReferenceHelper.FormatQuoteReference(new LocalDate(2026, 1, 15), 1_234_567);

        reference.Should().Be("TRZ-260115-1234567");
    }
}
