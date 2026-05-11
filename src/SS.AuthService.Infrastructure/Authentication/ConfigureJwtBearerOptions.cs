using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SS.AuthService.Application.Interfaces;
using System.Text;

namespace SS.AuthService.Infrastructure.Authentication;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IRsaKeyProvider _rsaKeyProvider;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions, IRsaKeyProvider rsaKeyProvider)
    {
        _jwtOptions = jwtOptions;
        _rsaKeyProvider = rsaKeyProvider;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var jwtSettings = _jwtOptions.Value;
        var key = _rsaKeyProvider.GetPublicKey();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = key,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ClockSkew = TimeSpan.FromSeconds(30) // Allow small clock drift between services
        };
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }
}
