using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class PayLaterDependencyInjection
{
    public static IServiceCollection AddPayLaterInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPayLaterRedis(configuration);
        services.AddSingleton<IPayLaterChargeScheduler, PayLaterChargeScheduler>();

        return services;
    }
}
