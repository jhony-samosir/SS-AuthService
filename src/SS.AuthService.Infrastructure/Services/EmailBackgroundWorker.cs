using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Interfaces;

namespace SS.AuthService.Infrastructure.Services;

public class EmailBackgroundWorker : BackgroundService
{
    private readonly IEmailQueue _emailQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailBackgroundWorker> _logger;

    public EmailBackgroundWorker(
        IEmailQueue emailQueue, 
        IServiceProvider serviceProvider, 
        ILogger<EmailBackgroundWorker> logger)
    {
        _emailQueue = emailQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Background Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var emailTask = await _emailQueue.DequeueEmailAsync(stoppingToken);

                // Scope baru karena IEmailService mungkin Scoped (GmailEmailService adalah Scoped)
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                _logger.LogInformation("Processing {EmailType} email for {Email}", emailTask.Type, emailTask.To);
                
                if (emailTask.Type == EmailType.Verification)
                {
                    await emailService.SendVerificationEmailAsync(emailTask.To, emailTask.Token!);
                }
                else if (emailTask.Type == EmailType.MfaRecoveryCodes)
                {
                    await emailService.SendMfaRecoveryCodesEmailAsync(emailTask.To, emailTask.Codes!);
                }

                _logger.LogInformation("Email successfully sent to {Email}", emailTask.To);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email from background queue.");
            }
        }

        _logger.LogInformation("Email Background Worker is stopping.");
    }
}
