using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SS.AuthService.Infrastructure.Authentication;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var jwtSettings = _jwtOptions.Value;

        var rsa = System.Security.Cryptography.RSA.Create();
        var pemContent = System.IO.File.ReadAllText(jwtSettings.PublicKeyPath);
        rsa.ImportFromPem(pemContent);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ClockSkew = TimeSpan.FromSeconds(30) // Allow small clock drift between services
        };
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }
}
