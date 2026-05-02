using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
