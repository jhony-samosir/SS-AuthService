using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Messaging;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(IServiceProvider serviceProvider, ILogger<OutboxWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEvents(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("OutboxWorker stopped.");
    }

    private async Task ProcessPendingEvents(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMQPublisher>();

        // EF Core 8+ allows ExecuteUpdate, but we need to fetch them.
        // We fetch pending events. Ideally, we should use FOR UPDATE SKIP LOCKED.
        // Since PostgreSQL is used, we can use raw SQL for safe fetching.
        var pendingEvents = await dbContext.OutboxEvents
            .FromSqlRaw("SELECT * FROM outbox_events WHERE status = 'pending' AND retry_count < 5 ORDER BY created_at ASC LIMIT 50 FOR UPDATE SKIP LOCKED")
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        foreach (var evt in pendingEvents)
        {
            try
            {
                var routingKey = evt.EventType.ToLower().Replace("userregistered", "auth.user.registered").Replace("userverified", "auth.user.verified");
                if (!routingKey.StartsWith("auth."))
                {
                    routingKey = "auth." + evt.EventType.ToLower();
                }

                // Since payload is a JSON string, we can publish it as-is if IRabbitMQPublisher supports raw bytes, 
                // but IRabbitMQPublisher serializes T. So we pass an object by deserializing first.
                var payloadObj = System.Text.Json.JsonSerializer.Deserialize<object>(evt.Payload);

                var messageId = $"auth-event-{evt.Id}";
                var correlationId = evt.AggregateId.ToString();

                await publisher.PublishAsync(routingKey, payloadObj, evt.EventType, messageId, correlationId);

                evt.Status = "published";
                evt.PublishedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId}", evt.Id);
                evt.RetryCount++;
                evt.ErrorMessage = ex.Message;
                if (evt.RetryCount >= 5)
                {
                    evt.Status = "failed";
                }
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
