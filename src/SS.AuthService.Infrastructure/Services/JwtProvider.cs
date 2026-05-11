using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Authentication;
using SS.AuthService.Application.Common.Constants;

namespace SS.AuthService.Infrastructure.Services;

/// <summary>
/// Implementasi pembuatan JWT Access Token dan Refresh Token.
/// Menggunakan kunci asimetris (RSA) untuk penandatanganan token.
/// </summary>
public class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    private readonly IRsaKeyProvider _rsaKeyProvider;

    public JwtProvider(IOptions<JwtOptions> options, IRsaKeyProvider rsaKeyProvider)
    {
        _options = options.Value;
        _rsaKeyProvider = rsaKeyProvider;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimConstants.UserId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("full_name", user.FullName),
            new(ClaimConstants.PublicId, user.PublicId.ToString()),
            new(ClaimConstants.Role, user.RoleId.ToString())
        };

        // Standardize: Add role names if available (Validation should happen in Application layer)
        if (user.Role != null)
        {
            claims.Add(new Claim("role_name", user.Role.Name));
            claims.Add(new Claim("role_public_id", user.Role.PublicId.ToString()));
        }

        return GenerateToken(claims, _options.AccessTokenExpirationMinutes);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string GenerateMfaChallengeToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("mfa_pending", "true"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims, 5); // Short-lived for challenge
    }

    public int? ValidateMfaChallengeToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var key = _rsaKeyProvider.GetPublicKey();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            
            var mfaPending = jwtToken.Claims.FirstOrDefault(x => x.Type == "mfa_pending")?.Value;
            if (mfaPending != "true") return null;

            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(userId, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims, int expirationMinutes)
    {
        var key = _rsaKeyProvider.GetPrivateKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
