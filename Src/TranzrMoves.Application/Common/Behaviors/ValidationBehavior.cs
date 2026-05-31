using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace TranzrMoves.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IServiceScopeFactory scopeFactory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IErrorOr
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<TRequest>>().ToList();
        if (validators.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(message);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(result => result.Errors).ToList();
        if (failures.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        var errors = failures
            .ConvertAll(validationFailure => Error.Validation(validationFailure.PropertyName, validationFailure.ErrorMessage));

        return (dynamic)errors;
    }
}
