using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Infrastructure.Persistence.Context;
using SS.AuthService.Infrastructure.Repositories;

namespace SS.AuthService.Infrastructure;

/// <summary>
/// Implementasi Unit of Work yang membungkus transaksi EF Core.
/// Application layer tidak mengenal EF Core sama sekali — hanya tahu IUnitOfWork.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private IUserRepository? _users;
    private IEmailVerificationRepository? _emailVerifications;
    private ILoginAttemptRepository? _loginAttempts;
    private IAuthSessionRepository? _authSessions;
    private IRoleMenuRepository? _roleMenus;
    private IPasswordResetRepository? _passwordResets;
    private IPasswordHistoryRepository? _passwordHistories;
    private IMfaRecoveryCodeRepository? _mfaRecoveryCodes;
    private IRoleRepository? _roles;

    private readonly IMemoryCache _cache;
    private readonly ILoggerFactory _loggerFactory;

    public UnitOfWork(AppDbContext context, IMemoryCache cache, ILoggerFactory loggerFactory)
    {
        _context = context;
        _cache = cache;
        _loggerFactory = loggerFactory;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IEmailVerificationRepository EmailVerifications => _emailVerifications ??= new EmailVerificationRepository(_context);
    public ILoginAttemptRepository LoginAttempts => _loginAttempts ??= new LoginAttemptRepository(_context);
    public IAuthSessionRepository AuthSessions => _authSessions ??= new AuthSessionRepository(_context);
    public IRoleMenuRepository RoleMenus => _roleMenus ??= new RoleMenuRepository(_context, _cache, _loggerFactory.CreateLogger<RoleMenuRepository>());
    public IPasswordResetRepository PasswordResets => _passwordResets ??= new PasswordResetRepository(_context);
    public IPasswordHistoryRepository PasswordHistories => _passwordHistories ??= new PasswordHistoryRepository(_context);
    public IMfaRecoveryCodeRepository MfaRecoveryCodes => _mfaRecoveryCodes ??= new MfaRecoveryCodeRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) throw new InvalidOperationException("No active transaction to commit.");
        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;
        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }
}
