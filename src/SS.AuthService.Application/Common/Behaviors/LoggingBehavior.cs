using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SS.AuthService.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior to log the execution of requests in the Application layer.
/// It tracks the start, successful completion, and failures with exception details and elapsed duration.
/// To protect sensitive data and PII, it does not log request/response payloads.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling MediatR request: {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled MediatR request successfully: {RequestName} in {DurationMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed handling MediatR request: {RequestName} after {DurationMs}ms with exception: {ExceptionMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
