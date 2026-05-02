using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly AppDbContext _context;

    public PasswordResetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordReset reset, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResets.AddAsync(reset, cancellationToken);
    }

    public async Task<PasswordReset?> GetByTokenHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResets
            .FirstOrDefaultAsync(pr => pr.ResetTokenHash == hash, cancellationToken);
    }

    public void Update(PasswordReset reset)
    {
        _context.PasswordResets.Update(reset);
    }
}
