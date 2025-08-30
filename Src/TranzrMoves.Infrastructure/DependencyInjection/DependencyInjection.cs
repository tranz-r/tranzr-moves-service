using Amazon.Runtime;
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
        var awsOption = configuration.GetAWSOptions();
        awsOption.Credentials = new BasicAWSCredentials(configuration["AWS_ACCESS_KEY_ID"], configuration["AWS_SECRET_ACCESS_KEY"]);
        services.AddDefaultAWSOptions(awsOption);
        
        
        // services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();
        services.AddSingleton<IAwsEmailService, AwsEmailService>();
        
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserQuoteRepository, UserQuoteRepository>();
        services.AddTransient<IDriverQuoteRepository, DriverQuoteRepository>();
        services.AddTransient<IQuoteRepository, QuoteRepository>();

        services.AddTransient<IRemovalPricingRepository, RemovalPricingRepository>();
        services.AddTransient<IRateCardRepository, RateCardRepository>();
        services.AddTransient<IServiceFeatureRepository, ServiceFeatureRepository>();
        services.AddTransient<IAdditionalPriceRepository, AdditionalPriceRepository>();

        return services;
    }
}
