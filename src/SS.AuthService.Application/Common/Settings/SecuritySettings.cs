namespace SS.AuthService.Application.Common.Settings;

public class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    public int PasswordResetExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 7;
    public int ConstantTimeResponseMs { get; init; } = 1000;
}
