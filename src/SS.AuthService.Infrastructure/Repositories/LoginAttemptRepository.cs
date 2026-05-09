using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class LoginAttemptRepository : ILoginAttemptRepository
{
    private readonly AppDbContext _context;

    public LoginAttemptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.LoginAttempts.AddAsync(attempt, cancellationToken);
    }

    public async Task<(IReadOnlyList<LoginAttempt> Items, int TotalCount)> GetPagedAsync(
        string? email = null,
        int? userId = null,
        string? ipAddress = null,
        bool? isSuccess = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LoginAttempts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(x => x.EmailAttempted == email);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
                query = query.Where(x => x.IpAddress == ip);
        }

        if (isSuccess.HasValue)
            query = query.Where(x => x.IsSuccess == isSuccess.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.AttemptedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.AttemptedAt <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.AttemptedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<LoginAttempt?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.LoginAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
