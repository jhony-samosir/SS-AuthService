using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users.AnyAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.PublicId == publicId && u.DeletedAt == null, cancellationToken);

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        => await _context.Users.AddAsync(entity, cancellationToken);

    public void Update(User entity)
        => _context.Users.Update(entity);

    public async Task<int> GetDefaultCustomerRoleIdAsync(CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == "Customer" && r.DeletedAt == null, cancellationToken);

        return role?.Id
            ?? throw new InvalidOperationException(
                "Default 'Customer' role not found. Ensure the database has been seeded properly.");
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        SS.AuthService.Application.Users.Queries.UserFilter filter, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .Where(u => u.DeletedAt == null);

        // Filtering
        if (!string.IsNullOrWhiteSpace(filter.Email))
            query = query.Where(u => EF.Functions.ILike(u.Email, $"%{filter.Email}%"));

        if (!string.IsNullOrWhiteSpace(filter.FullName))
            query = query.Where(u => EF.Functions.ILike(u.FullName, $"%{filter.FullName}%"));

        if (filter.RoleId.HasValue)
            query = query.Where(u => u.RoleId == filter.RoleId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        if (filter.MfaEnabled.HasValue)
            query = query.Where(u => u.MfaEnabled == filter.MfaEnabled.Value);

        if (filter.IsLocked.HasValue)
        {
            var now = DateTime.UtcNow;
            query = filter.IsLocked.Value
                ? query.Where(u => u.LockedUntil.HasValue && u.LockedUntil.Value > now)
                : query.Where(u => !u.LockedUntil.HasValue || u.LockedUntil.Value <= now);
        }

        if (filter.CreatedAtFrom.HasValue)
            query = query.Where(u => u.CreatedAt >= filter.CreatedAtFrom.Value);

        if (filter.CreatedAtTo.HasValue)
            query = query.Where(u => u.CreatedAt <= filter.CreatedAtTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = filter.SortBy.ToLower() switch
        {
            "email" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "fullname" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName),
            "rolename" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.Role.Name) : query.OrderByDescending(u => u.Role.Name),
            "isactive" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.IsActive) : query.OrderByDescending(u => u.IsActive),
            "mfaenabled" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.MfaEnabled) : query.OrderByDescending(u => u.MfaEnabled),
            "updatedat" => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.UpdatedAt) : query.OrderByDescending(u => u.UpdatedAt),
            _ => filter.SortDirection.ToLower() == "asc" ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
        };

        // Paging
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default)
        => await _context.Roles.AnyAsync(r => r.Id == roleId && r.DeletedAt == null, cancellationToken);

    public void Delete(User entity)
        => _context.Users.Remove(entity);
}
