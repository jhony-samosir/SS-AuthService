namespace SS.AuthService.Application.Interfaces;

/// <summary>
/// Abstraksi Unit of Work agar Application layer bebas dari EF Core.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IEmailVerificationRepository EmailVerifications { get; }
    ILoginAttemptRepository LoginAttempts { get; }
    IAuthSessionRepository AuthSessions { get; }
    IRoleMenuRepository RoleMenus { get; }
    IPasswordResetRepository PasswordResets { get; }
    IPasswordHistoryRepository PasswordHistories { get; }
    IMfaRecoveryCodeRepository MfaRecoveryCodes { get; }
    IRoleRepository Roles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
