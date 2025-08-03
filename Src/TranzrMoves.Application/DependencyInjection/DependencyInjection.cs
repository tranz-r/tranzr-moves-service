using System.Net;
using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Application.Common.Behaviors;

namespace TranzrMoves.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); 
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Ping)];
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });
    
        return services;
    }
}

public sealed record Ping(Guid Id) : IRequest<Pong>;

public sealed record Pong(Guid Id);

public sealed class PingHandler : IRequestHandler<Ping, Pong>
{
    public ValueTask<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        return new ValueTask<Pong>(new Pong(request.Id));
    }
}
