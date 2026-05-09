using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AppDbContext _context;

    public AuthSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        await _context.AuthSessions.AddAsync(session, cancellationToken);
    }

    public async Task<AuthSession?> GetByRefreshTokenHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .FirstOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);
    }

    public void Revoke(AuthSession session)
    {
        session.IsRevoked = true;
        session.UpdatedAt = DateTime.UtcNow;
        _context.AuthSessions.Update(session);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.AuthSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.IsRevoked, true)
                .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), 
                cancellationToken);
    }

    public async Task<List<AuthSession>> GetByUserIdAsync(int userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthSession?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .FirstOrDefaultAsync(s => s.PublicId == publicId, cancellationToken);
    }
}
