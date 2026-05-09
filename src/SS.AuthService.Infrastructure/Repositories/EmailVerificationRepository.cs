using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class EmailVerificationRepository : IEmailVerificationRepository
{
    private readonly AppDbContext _context;

    public EmailVerificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerification entity, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerifications.AddAsync(entity, cancellationToken);
    }

    public async Task<EmailVerification?> GetByTokenHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerifications
            .FirstOrDefaultAsync(v => v.VerificationTokenHash == hash, cancellationToken);
    }

    public void Remove(EmailVerification entity)
    {
        _context.EmailVerifications.Remove(entity);
    }

    public async Task RemoveAllByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerifications
            .Where(v => v.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<EmailVerification?> GetLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerifications
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
