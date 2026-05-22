using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace SS.AuthService.API.Middlewares;

/// <summary>
/// Middleware to propagate X-Correlation-Id from API Gateway to Serilog LogContext.
/// It retrieves the correlation ID from the request headers and injects it into
/// Serilog's LogContext so that all log messages contain the CorrelationId property.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();

        // Propagate back to response headers if present
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Response.Headers[CorrelationHeader] = correlationId;
        }

        // Push to Serilog LogContext
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
