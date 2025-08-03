using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace TranzrMoves.Application.Common.Behaviors;

public class LoggingBehavior<TMessage, TResponse>(ILogger<Mediator.Mediator> logger) : IPipelineBehavior<TMessage, TResponse> where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = message.GetType().Name;
        var requestGuid = Guid.NewGuid().ToString();

        var requestNameWithGuid = $"{requestName} [{requestGuid}]";

        logger.LogInformation($"[START] {requestNameWithGuid}");
        TResponse response;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            try
            {
                logger.LogInformation($"[PROPS] {requestNameWithGuid} {@message}");
            }
            catch (NotSupportedException)
            {
                logger.LogInformation($"[Serialization ERROR] {requestNameWithGuid} Could not serialize the request.");
            }

            response = await next(message, cancellationToken);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                $"[END] {requestNameWithGuid}; Execution time={stopwatch.ElapsedMilliseconds}ms");
        }

        return response;
    }
}