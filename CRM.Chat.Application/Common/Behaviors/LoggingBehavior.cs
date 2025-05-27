using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling request {RequestName}", requestName);

        try
        {
            var result = await next();

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Request {RequestName} handled successfully in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Request {RequestName} failed in {ElapsedMs}ms. Error: {Error}",
                    requestName, stopwatch.ElapsedMilliseconds, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} threw exception in {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}