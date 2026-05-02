using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IPasswordResetRepository
{
    Task AddAsync(PasswordReset reset, CancellationToken cancellationToken = default);
    Task<PasswordReset?> GetByTokenHashAsync(string hash, CancellationToken cancellationToken = default);
    void Update(PasswordReset reset);
}
