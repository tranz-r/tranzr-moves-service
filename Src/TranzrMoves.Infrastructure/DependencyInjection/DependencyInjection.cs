using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Application.Features.Admin.Dashboard;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Respositories;
using TranzrMoves.Infrastructure.Services;
using TranzrMoves.Infrastructure.Services.EmailTemplates;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var connectionString = configuration["COMMUNICATION_SERVICES_CONNECTION_STRING"];
            return new EmailClient(connectionString);
        });

        services.AddSingleton<IEmailService, AzureEmailService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<ITurnstileService, TurnstileService>();

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserQuoteRepository, UserQuoteRepository>();
        services.AddTransient<IDriverQuoteRepository, DriverQuoteRepository>();
        services.AddTransient<IQuoteRepository, QuoteRepository>();

        services.AddTransient<IRemovalPricingRepository, RemovalPricingRepository>();
        services.AddTransient<IRateCardRepository, RateCardRepository>();
        services.AddTransient<IServiceFeatureRepository, ServiceFeatureRepository>();
        services.AddTransient<IAdditionalPriceRepository, AdditionalPriceRepository>();
        services.AddTransient<ILegalDocumentRepository, LegalDocumentRepository>();

        // Azure Blob Storage Service
        services.AddTransient<IAzureBlobService, AzureBlobService>();

        // Admin Services
        services.AddTransient<IAdminMetricsService, AdminMetricsService>();

        services.AddTransient<IInventoryItemRepository, InventoryItemRepository>();

        return services;
    }
}
