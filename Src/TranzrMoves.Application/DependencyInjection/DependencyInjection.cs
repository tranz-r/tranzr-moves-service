using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

using TranzrMoves.Application.Common.Behaviors;
using TranzrMoves.Application.Common.Strategy;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Features.Quote.Journey.Init;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(_ => SystemClock.Instance);
        services.AddSingleton<ITimeService, TimeService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediator(options =>
            {
                options.Assemblies = [typeof(InitQuoteJourneyCommand).Assembly];
                options.ServiceLifetime = ServiceLifetime.Scoped;
            }
        );

        services
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IQuoteJourneyProvider, QuoteJourneyProvider>();
        services.AddScoped<IQuoteProgressCalculator, QuoteProgressCalculator>();
        services.AddScoped<IQuoteResumeResolver, QuoteResumeResolver>();
        services.AddScoped<IQuoteResumeTokenService, QuoteResumeTokenService>();
        services.AddScoped<IQuoteStepInvalidationService, QuoteStepInvalidationService>();

        services.AddTransient<IPricingStrategy, PickAndDropPricingStrategy>();
        services.AddTransient<IPricingStrategy, RemovalPricingStrategy>();
        services.AddTransient<IPricingStrategyResolver, PricingStrategyResolver>();
        services.AddTransient<PricingContext>();

        return services;
    }
}
