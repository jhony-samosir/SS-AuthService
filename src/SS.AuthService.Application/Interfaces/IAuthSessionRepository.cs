using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session, CancellationToken cancellationToken = default);
    Task<AuthSession?> GetByRefreshTokenHashAsync(string hash, CancellationToken cancellationToken = default);
    void Revoke(AuthSession session);
    Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default);
}
