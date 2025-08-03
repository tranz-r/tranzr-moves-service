using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Respositories;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();
        services.AddSingleton<IAwsEmailService, AwsEmailService>();
        
        services.AddTransient<IUserRepository, UserRepository>();

        return services;
    }
}
