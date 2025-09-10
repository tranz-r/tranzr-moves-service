namespace TranzrMoves.Application.Contracts;

public record QuoteTypeDto
{
    public QuoteDto? Quote { get; init; }
    public string Etag { get; init; }
}