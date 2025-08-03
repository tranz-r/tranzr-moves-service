using ErrorOr;
using FluentValidation;
using Mediator;

namespace TranzrMoves.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validators = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if(validators is null)
        {
            return await next(message, cancellationToken);
        }

        var validationResult = await validators.ValidateAsync(message, cancellationToken);
        
        if(validationResult.IsValid)
        {
            return await next(message, cancellationToken);
        }

        var errors = validationResult.Errors
            .ConvertAll(validationFailure => Error.Validation(validationFailure.PropertyName, validationFailure.ErrorMessage));
        
        return (dynamic)errors;
    }
}