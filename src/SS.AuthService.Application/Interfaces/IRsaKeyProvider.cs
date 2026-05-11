using Microsoft.IdentityModel.Tokens;

namespace SS.AuthService.Application.Interfaces;

public interface IRsaKeyProvider
{
    RsaSecurityKey GetPublicKey();
    RsaSecurityKey GetPrivateKey();
}
