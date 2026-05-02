using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly AppDbContext _context;

    public PasswordHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordHistory history, CancellationToken cancellationToken = default)
    {
        await _context.PasswordHistories.AddAsync(history, cancellationToken);
    }

    public async Task<List<PasswordHistory>> GetLastPasswordsAsync(int userId, int count, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
