using System.Text;

using NodaTime.Text;

namespace TranzrMoves.Application.Helpers;

public static class BookingNumberGenerator
{
    /// <param name="utcToday">Calendar date in UTC for the booking reference segment.</param>
    public static string Generate(LocalDate utcToday)
    {
        string prefix = "TRZ";
        string datePart = LocalDatePattern.CreateWithInvariantCulture("yyMMdd")
            .Format(utcToday);

        // Get 64-bit value from UUID
        var guidBytes = Guid.NewGuid().ToByteArray();
        ulong number = BitConverter.ToUInt64(guidBytes, 0);

        // Base36 encode, take first 6 characters
        string shortId = Base36Encode(number).Substring(0, 6).ToUpper();

        return $"{prefix}-{datePart}-{shortId}";
    }

    private static string Base36Encode(ulong value)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = new StringBuilder();

        do
        {
            result.Insert(0, chars[(int)(value % 36)]);
            value /= 36;
        } while (value != 0);

        return result.ToString();
    }
}
