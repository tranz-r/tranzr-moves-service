using System.Text;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Prices.Removals;

public class RemovalPricesRequestHandler(
    IRemovalPricingRepository removalPricingRepository,
    ILogger<RemovalPricesRequestHandler> logger)
    : IRequestHandler<RemovalPricesRequest, ErrorOr<RemovalPricingDto>>
{
    public async ValueTask<ErrorOr<RemovalPricingDto>> Handle(
        RemovalPricesRequest request,
        CancellationToken cancellationToken)
    {
        var rates = await removalPricingRepository.GetRateCardsAsync(request.At, cancellationToken);
        
        string currency = rates.FirstOrDefault()?.CurrencyCode ?? "GBP";

        RateLeafDto? Map(int movers, ServiceLevel level) =>
            rates.Where(r => r.Movers == movers && r.ServiceLevel == level)
                .Select(r => new RateLeafDto
                {
                    BaseBlockHours = r.BaseBlockHours,
                    BaseBlockPrice = r.BaseBlockPrice,
                    HourlyAfter = r.HourlyRateAfter
                })
                .FirstOrDefault();
        
        var features = await removalPricingRepository.GetServiceFeatureAsync(request.At, cancellationToken);

        var version = ComputePayloadVersion(rates, features);

        var standardTexts = features.Where(f => f.ServiceLevel == ServiceLevel.Standard)
            .Select((f, i) => new ServiceTextDto { Id = i + 1, Text = f.Text })
            .ToList();

        var premiumTexts = features.Where(f => f.ServiceLevel == ServiceLevel.Premium)
            .Select((f, i) => new ServiceTextDto { Id = i + 1, Text = f.Text })
            .ToList();

        return new RemovalPricingDto
        {
            Version = version,
            Currency = currency,
            GeneratedAt = DateTimeOffset.UtcNow,
            Rates = new RatesDto
            {
                One = new MoversDto
                    { Standard = Map(1, ServiceLevel.Standard), Premium = Map(1, ServiceLevel.Premium) },
                Two = new MoversDto
                    { Standard = Map(2, ServiceLevel.Standard), Premium = Map(2, ServiceLevel.Premium) },
                Three = new MoversDto
                    { Standard = Map(3, ServiceLevel.Standard), Premium = Map(3, ServiceLevel.Premium) },
                StandardServiceTexts = standardTexts,
                PremiumServiceTexts = premiumTexts
            }
        };
    }
    
    private static string ComputePayloadVersion(
        IEnumerable<RateCard> rates, IEnumerable<ServiceFeature> features)
    {
        var sb = new StringBuilder();

        // deterministic order + culture-invariant numeric formatting
        var ci = System.Globalization.CultureInfo.InvariantCulture;

        foreach (var r in rates.OrderBy(r => r.Movers).ThenBy(r => r.ServiceLevel))
        {
            sb.Append(r.Movers).Append('|')
                .Append((int)r.ServiceLevel).Append('|')
                .Append(r.BaseBlockHours).Append('|')
                .Append(r.BaseBlockPrice.ToString(ci)).Append('|')
                .Append(r.HourlyRateAfter.ToString(ci)).Append('|')
                .Append(r.CurrencyCode).Append('|')
                .Append(r.IsActive ? '1' : '0').Append('|')
                .Append(r.EffectiveFrom.UtcDateTime.ToString("O")).Append('|')
                .Append(r.EffectiveTo?.UtcDateTime.ToString("O") ?? "null")
                .Append(';');
        }

        foreach (var f in features.OrderBy(f => f.ServiceLevel).ThenBy(f => f.DisplayOrder))
        {
            sb.Append((int)f.ServiceLevel).Append('|')
                .Append(f.DisplayOrder).Append('|')
                .Append(f.Text).Append('|')
                .Append(f.IsActive ? '1' : '0').Append('|')
                .Append(f.EffectiveFrom.UtcDateTime.ToString("O")).Append('|')
                .Append(f.EffectiveTo?.UtcDateTime.ToString("O") ?? "null")
                .Append(';');
        }

        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash).Substring(0, 12).ToLowerInvariant(); // e.g. "a1b2c3d4e5f6"
    }
}