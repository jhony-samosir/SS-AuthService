namespace SS.AuthService.Application.Common.Settings;

public class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    public int PasswordResetExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 7;
    public int ConstantTimeResponseMs { get; init; } = 1000;

    // Networking & Integration
    public string[] TrustedProxies { get; init; } = Array.Empty<string>();
    public string[] TrustedNetworks { get; init; } = Array.Empty<string>();
    public string[] AllowedCorsOrigins { get; init; } = Array.Empty<string>();
    public string HealthCheckDiskPath { get; init; } = "/";

    // Data Retention Policies
    public int SessionRetentionDays { get; init; } = 30;
    public int VerificationRetentionDays { get; init; } = 7;
    public int LoginAttemptRetentionDays { get; init; } = 90;
    public int CleanupIntervalHours { get; init; } = 24;
}
