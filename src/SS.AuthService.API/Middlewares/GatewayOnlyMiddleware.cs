using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace SS.AuthService.API.Middlewares;

/// <summary>
/// Defense-in-Depth Middleware:
/// Ensures that in Production, only requests proxied by the API Gateway are allowed.
/// Direct access to the microservice is rejected with 403 Forbidden.
/// </summary>
public class GatewayOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public GatewayOnlyMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Only enforce strict boundary in Production
        if (_env.IsProduction())
        {
            // 2. Always allow health checks (required for Docker/K8s probes)
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // 3. Verify the request is coming through a proxy (Gateway)
            // In a hardened environment, we would also verify the X-Internal-Signature HMAC.
            // For now, we check for the X-Forwarded-For header which is injected by our Gateway.
            if (!context.Request.Headers.ContainsKey("X-Forwarded-For") && 
                !context.Request.Headers.ContainsKey("X-Internal-Signature"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Security Violation: Direct access to this microservice is prohibited in production. Requests must flow through the API Gateway.");
                return;
            }
        }

        await _next(context);
    }
}
