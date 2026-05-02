using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IPasswordHistoryRepository
{
    Task AddAsync(PasswordHistory history, CancellationToken cancellationToken = default);
    Task<List<PasswordHistory>> GetLastPasswordsAsync(int userId, int count, CancellationToken cancellationToken = default);
}
