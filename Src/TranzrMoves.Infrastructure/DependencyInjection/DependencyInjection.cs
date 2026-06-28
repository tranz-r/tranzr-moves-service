using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stripe;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Respositories;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITurnstileService, TurnstileService>();
        services.Configure<SupabaseAuthOptions>(o =>
        {
            o.Url = configuration["SUPABASE_URL"];
            o.ServiceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"];
            o.InviteRedirectUrl = configuration["BUSINESS_PORTAL_INVITE_REDIRECT_URL"];
        });
        services.AddSingleton<ISupabaseAuthAdminService, SupabaseAuthAdminService>();
        services.AddScoped<INotificationPublisher, WolverineNotificationPublisher>();

        services.AddScoped<IQuoteV2HostedCheckoutSessionService, QuoteV2HostedCheckoutSessionService>();
        services.AddScoped<IQuoteV2LaterBalanceCollectionService, QuoteV2LaterBalanceCollectionService>();
        services.AddScoped<IQuoteV2DepositBalanceCollectionService, QuoteV2DepositBalanceCollectionService>();
        services.AddScoped<IQuoteV2PaymentSheetService, QuoteV2PaymentSheetService>();
        services.AddScoped<ICheckoutStripeReadService, CheckoutStripeReadService>();
        services.AddScoped<IQuoteV2DepositBalancePaymentService, QuoteV2DepositBalancePaymentService>();
        services.AddScoped<ICheckoutStripeWebhookV2Service>(sp => new CheckoutStripeWebhookV2Service(
            sp.GetRequiredService<StripeClient>(),
            configuration["TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2"] ?? string.Empty,
            sp.GetRequiredService<IQuoteRepository>(),
            sp.GetRequiredService<IQuoteProgressCalculator>(),
            sp.GetRequiredService<INotificationPublisher>(),
            sp.GetRequiredService<ITimeService>(),
            sp.GetRequiredService<IBalanceChargeScheduler>(),
            sp.GetRequiredService<ILogger<CheckoutStripeWebhookV2Service>>()));

        services.AddPayLaterInfrastructure(configuration);

        services.AddTransient<IUserV2Repository, UserV2Repository>();
        services.AddTransient<IBusinessAccountRepository, BusinessAccountRepository>();
        services.AddTransient<IBusinessUserRepository, BusinessUserRepository>();
        services.AddTransient<IQuoteRepository, QuoteRepository>();

        services.AddTransient<IRemovalPricingRepository, RemovalPricingRepository>();
        services.AddTransient<IRateCardRepository, RateCardRepository>();
        services.AddTransient<IServiceFeatureRepository, ServiceFeatureRepository>();
        services.AddTransient<IAdditionalPriceRepository, AdditionalPriceRepository>();
        services.AddTransient<ILegalDocumentRepository, LegalDocumentRepository>();

        services.AddTransient<IAzureBlobService, AzureBlobService>();

        services.AddTransient<IInventoryItemRepository, InventoryItemRepository>();

        return services;
    }
}
