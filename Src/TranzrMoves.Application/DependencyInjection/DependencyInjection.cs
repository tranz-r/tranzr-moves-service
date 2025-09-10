using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Application.Common.Behaviors;
using TranzrMoves.Application.Features.Quote.SelectQuoteType;

namespace TranzrMoves.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); 
        services.AddMediator( options =>
            {
                options.Assemblies = [typeof(SelectQuoteTypeCommand).Assembly];
                options.ServiceLifetime = ServiceLifetime.Scoped;
            }
        );
        
        services
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    
        return services;
    }
}
