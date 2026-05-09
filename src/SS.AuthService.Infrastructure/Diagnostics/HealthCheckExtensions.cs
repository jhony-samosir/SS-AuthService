using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;
using System.Text.Json;

namespace SS.AuthService.Infrastructure.Diagnostics;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddSecurityHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var diskPath = configuration["SecuritySettings:HealthCheckDiskPath"];
        if (string.IsNullOrEmpty(diskPath))
        {
            diskPath = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? "C:\\" : "/";
        }

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("SSAuthDB")!,
                name: "PostgreSQL",
                tags: new[] { "db", "sql", "postgresql" })
            .AddWorkingSetHealthCheck(1024 * 1024 * 1024, name: "Memory")
            .AddDiskStorageHealthCheck(setup => 
                setup.AddDrive(diskPath, 1024 * 1024 * 1024), // Min 1GB
                name: "Disk");

        return services;
    }

    public static IApplicationBuilder UseSecurityHealthChecks(this IApplicationBuilder app)
    {
        // Public Health Check (Plain Text)
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false, // Only check if service is alive
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(report.Status.ToString());
            }
        });

        // Detailed Health Check (JSON, Restricted)
        app.UseHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse,
            // Security: In production, this should be restricted by IP or Middleware
            // For now, we assume middleware/filter will handle it or we can check IP here
        });

        return app;
    }

    private static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            info = report.Entries.Select(e => new
            {
                key = e.Key,
                description = e.Value.Description,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
    }
}
