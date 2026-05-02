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
        => await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId && u.DeletedAt == null, cancellationToken);

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
}
