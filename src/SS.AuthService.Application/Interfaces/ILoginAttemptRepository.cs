using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface ILoginAttemptRepository
{
    Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<LoginAttempt> Items, int TotalCount)> GetPagedAsync(
        string? email = null,
        int? userId = null,
        string? ipAddress = null,
        bool? isSuccess = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<LoginAttempt?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
