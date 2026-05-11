using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.Infrastructure.Services;

public class RsaKeyProvider : IRsaKeyProvider
{
    private readonly Lazy<RsaSecurityKey> _publicKey;
    private readonly Lazy<RsaSecurityKey> _privateKey;

    public RsaKeyProvider(IOptions<JwtOptions> options)
    {
        var settings = options.Value;

        _publicKey = new Lazy<RsaSecurityKey>(() =>
        {
            using var rsa = RSA.Create();
            var pemContent = File.ReadAllText(settings.PublicKeyPath);
            rsa.ImportFromPem(pemContent);
            var rsaParams = rsa.ExportParameters(false);
            return new RsaSecurityKey(rsaParams);
        });

        _privateKey = new Lazy<RsaSecurityKey>(() =>
        {
            using var rsa = RSA.Create();
            var pemContent = File.ReadAllText(settings.PrivateKeyPath);
            rsa.ImportFromPem(pemContent);
            var rsaParams = rsa.ExportParameters(true);
            return new RsaSecurityKey(rsaParams);
        });
    }

    public RsaSecurityKey GetPublicKey() => _publicKey.Value;
    public RsaSecurityKey GetPrivateKey() => _privateKey.Value;
}
