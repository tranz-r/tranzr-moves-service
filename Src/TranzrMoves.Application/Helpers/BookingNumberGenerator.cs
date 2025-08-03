using System.Text;

namespace TranzrMoves.Application.Helpers;

public static class BookingNumberGenerator
{
    public static string Generate()
    {
        string prefix = "TRZ";
        string datePart = DateTime.UtcNow.ToString("yyMMdd");

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
