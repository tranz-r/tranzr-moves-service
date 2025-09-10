namespace TranzrMoves.Application.Contracts;

public record SaveQuoteRequest
{
    public QuoteDto Quote { get; set; } = null!;
    public UserDto? Customer { get; set; }
    public string? ETag { get; set; }
}
