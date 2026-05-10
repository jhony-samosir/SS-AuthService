using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.BackgroundServices;

public class SecurityDataRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityDataRetentionWorker> _logger;
    private readonly SecuritySettings _settings;

    public SecurityDataRetentionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityDataRetentionWorker> logger,
        IOptions<SecuritySettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security Data Retention Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.CleanupIntervalHours));

        try
        {
            // Initial cleanup on startup
            await PerformCleanupAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Security Data Retention Worker is stopping.");
        }
    }

    private async Task PerformCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting automated security data cleanup...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            const int batchSize = 5000;

            // 1. Auth Sessions: Hard delete revoked sessions older than 30 days
            var sessionThreshold = now.AddDays(-_settings.SessionRetentionDays);
            int totalDeletedSessions = 0;
            int deleted;
            
            do
            {
                deleted = await context.AuthSessions
                    .IgnoreQueryFilters()
                    .Where(x => x.IsRevoked && x.ExpiresAt < sessionThreshold)
                    .OrderBy(x => x.Id)
                    .Take(batchSize)
                    .ExecuteDeleteAsync(ct);
                totalDeletedSessions += deleted;
            } while (deleted == batchSize && !ct.IsCancellationRequested);

            // 2. Email Verifications & Password Resets: Hard delete expired > 7 days
            var verificationThreshold = now.AddDays(-_settings.VerificationRetentionDays);
            
            int totalDeletedEmailVer = 0;
            do
            {
                deleted = await context.EmailVerifications
                    .IgnoreQueryFilters()
                    .Where(x => x.ExpiresAt < verificationThreshold)
                    .OrderBy(x => x.Id)
                    .Take(batchSize)
                    .ExecuteDeleteAsync(ct);
                totalDeletedEmailVer += deleted;
            } while (deleted == batchSize && !ct.IsCancellationRequested);

            int totalDeletedPassRes = 0;
            do
            {
                deleted = await context.PasswordResets
                    .IgnoreQueryFilters()
                    .Where(x => x.ExpiresAt < verificationThreshold)
                    .OrderBy(x => x.Id)
                    .Take(batchSize)
                    .ExecuteDeleteAsync(ct);
                totalDeletedPassRes += deleted;
            } while (deleted == batchSize && !ct.IsCancellationRequested);

            // 3. Login Attempts: Hard delete older than 90 days
            var loginAttemptThreshold = now.AddDays(-_settings.LoginAttemptRetentionDays);
            int totalDeletedLoginAtt = 0;
            do
            {
                deleted = await context.LoginAttempts
                    .IgnoreQueryFilters()
                    .Where(x => x.AttemptedAt < loginAttemptThreshold)
                    .OrderBy(x => x.Id)
                    .Take(batchSize)
                    .ExecuteDeleteAsync(ct);
                totalDeletedLoginAtt += deleted;
            } while (deleted == batchSize && !ct.IsCancellationRequested);

            _logger.LogInformation(
                "Cleanup completed successfully. " +
                "Deleted: {Sessions} sessions, {EmailVer} email verifications, {PassRes} password resets, {LoginAtt} login attempts.",
                totalDeletedSessions, totalDeletedEmailVer, totalDeletedPassRes, totalDeletedLoginAtt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "An error occurred during security data cleanup.");
        }
    }
}
