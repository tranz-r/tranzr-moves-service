using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class QuoteContextDto
{
    public string? ActiveQuoteType { get; set; }
    public Dictionary<QuoteType, QuoteDto> Quotes { get; set; } = new();
    public QuoteContextMetadataDto Metadata { get; set; } = new();
    public UserDto? Customer { get; set; }
}

public class QuoteContextMetadataDto
{
    public string? LastActiveQuoteType { get; set; }
    public string LastActivity { get; set; } = DateTime.UtcNow.ToString("O");
    public string Version { get; set; } = "1.0.0";
}
