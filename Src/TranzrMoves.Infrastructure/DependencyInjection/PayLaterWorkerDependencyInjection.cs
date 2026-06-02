using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using StackExchange.Redis;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Respositories;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class PayLaterWorkerDependencyInjection
{
    public static IServiceCollection AddPayLaterWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeRedis,
        bool includeBalanceCollection)
    {
        services.AddSingleton<IClock>(_ => SystemClock.Instance);
        services.AddSingleton<ITimeService, TimeService>();
        services.Configure<PayLaterOptions>(configuration.GetSection(PayLaterOptions.SectionName));
        services.AddScoped<ICollectQuoteV2BalanceChargePublisher, CollectQuoteV2BalanceChargePublisher>();
        services.AddTransient<IQuoteRepository, QuoteRepository>();

        if (includeRedis)
        {
            services.AddPayLaterRedis(configuration);
        }

        if (includeBalanceCollection)
        {
            services.AddPayLaterBalanceCollection();
        }

        return services;
    }

    public static IServiceCollection AddPayLaterRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("Connection string 'redis' is not configured.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        return services;
    }

    public static IServiceCollection AddPayLaterBalanceCollection(this IServiceCollection services)
    {
        services.AddSingleton<ITimeService, TimeService>();
        services.AddScoped<INotificationPublisher, WolverineNotificationPublisher>();
        services.AddScoped<IQuoteV2HostedCheckoutSessionService, QuoteV2HostedCheckoutSessionService>();
        services.AddScoped<IQuoteV2LaterBalanceCollectionService, QuoteV2LaterBalanceCollectionService>();
        services.AddScoped<IQuoteV2DepositBalanceCollectionService, QuoteV2DepositBalanceCollectionService>();

        return services;
    }
}
