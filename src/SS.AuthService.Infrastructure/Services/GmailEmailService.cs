using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using SS.AuthService.Application.Interfaces;

namespace SS.AuthService.Infrastructure.Services;

public class GmailEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<GmailEmailService> _logger;

    public GmailEmailService(IOptions<EmailOptions> options, ILogger<GmailEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        // 1. Load Template (Best Practice: External File)
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "VerificationEmail.html");
        // Fallback jika file tidak ditemukan saat development/build
        string htmlBody;
        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath);
        }
        else
        {
            // Fallback template jika file hilang
            htmlBody = "<h2>Verify Email</h2><a href='{VerificationUrl}'>Verify</a>";
        }

        var verificationUrl = $"{_options.BaseUrl}/verify-email?token={token}";
        htmlBody = htmlBody.Replace("{VerificationUrl}", verificationUrl);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SamStore-Notification", _options.FromEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Verify Your Email - SS-AuthService";

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        // 2. Polly Retry Policy (Best Practice: Resiliency)
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Retry {RetryCount} sending email to {Email} after {TimeSpan}s due to error.", 
                        retryCount, email, timeSpan.TotalSeconds);
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpServer, _options.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.UserName, _options.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        });
    }

    public async Task SendMfaRecoveryCodesEmailAsync(string email, IEnumerable<string> codes)
    {
        var htmlBody = $@"
            <h2>New MFA Recovery Codes</h2>
            <p>Admin has regenerated your MFA recovery codes. Please store these in a secure place:</p>
            <ul>
                {string.Join("", codes.Select(c => $"<li><code>{c}</code></li>"))}
            </ul>
            <p>These codes replace any previous recovery codes.</p>";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SamStore-Security", _options.FromEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "MFA Recovery Codes Regenerated - SS-AuthService";

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        await retryPolicy.ExecuteAsync(async () =>
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpServer, _options.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.UserName, _options.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        });
    }
}
