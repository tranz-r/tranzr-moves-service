using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class QuoteReminderWorkerDependencyInjection
{
    public static IServiceCollection AddQuoteReminderWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<QuoteRemindersOptions>(configuration.GetSection(QuoteRemindersOptions.SectionName));
        services.AddScoped<INotificationPublisher, WolverineNotificationPublisher>();
        services.AddScoped<IQuoteResumeResolver, QuoteResumeResolver>();
        services.AddScoped<IQuoteResumeTokenService, QuoteResumeTokenService>();
        services.AddScoped<IQuoteJourneyProvider, QuoteJourneyProvider>();
        services.AddScoped<IQuoteProgressCalculator, QuoteProgressCalculator>();
        return services;
    }
}
