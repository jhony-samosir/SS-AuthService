using System.ComponentModel.DataAnnotations;

namespace SS.AuthService.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public string PrivateKeyPath { get; init; } = string.Empty;
    public string PublicKeyPath { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 15;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
