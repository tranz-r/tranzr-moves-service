namespace TranzrMoves.Application.Contracts;

public sealed class RateLeafDto
{
    public int BaseBlockHours { get; init; }
    public decimal BaseBlockPrice { get; init; }
    public decimal HourlyAfter { get; init; }
}

public sealed class MoversDto
{
    public RateLeafDto? Standard { get; init; }
    public RateLeafDto? Premium  { get; init; }
}

public sealed class ServiceTextDto
{
    public int Id { get; init; }
    public string Text { get; init; } = default!;
}

public sealed class RatesDto
{
    public MoversDto? One { get; init; }
    public MoversDto? Two { get; init; }
    public MoversDto? Three { get; init; }

    public List<ServiceTextDto> StandardServiceTexts { get; init; } = new();
    public List<ServiceTextDto> PremiumServiceTexts  { get; init; } = new();
}


public sealed class ExtraPricesDto
{
    public AdditionalPriceDto? Dismantle { get; init; }
    public AdditionalPriceDto? Assembly { get; init; }
}

public sealed record RemovalPricingDto
{
    public string Version { get; init; } = default!;
    public string Currency { get; init; } = "GBP";
    public DateTimeOffset GeneratedAt { get; init; }
    public required ExtraPricesDto ExtraPrice { get; init; }
    public RatesDto Rates { get; init; } = new();
}