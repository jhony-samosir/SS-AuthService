using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.Queries;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, cancellationToken);

    public async Task<Role?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => await _context.Roles.FirstOrDefaultAsync(r => r.PublicId == publicId && r.DeletedAt == null, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name && r.DeletedAt == null, cancellationToken);

    public async Task<(List<Role> Items, int TotalCount)> GetPagedAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Roles
            .AsNoTracking()
            .Where(r => r.DeletedAt == null);

        // Search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            dbQuery = dbQuery.Where(r => 
                EF.Functions.ILike(r.Name, $"%{query.SearchTerm}%") || 
                EF.Functions.ILike(r.Description ?? "", $"%{query.SearchTerm}%"));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Sorting
        dbQuery = query.SortBy.ToLower() switch
        {
            "name" => query.SortDirection.ToLower() == "asc" ? dbQuery.OrderBy(r => r.Name) : dbQuery.OrderByDescending(r => r.Name),
            "updatedat" => query.SortDirection.ToLower() == "asc" ? dbQuery.OrderBy(r => r.UpdatedAt) : dbQuery.OrderByDescending(r => r.UpdatedAt),
            _ => query.SortDirection.ToLower() == "asc" ? dbQuery.OrderBy(r => r.CreatedAt) : dbQuery.OrderByDescending(r => r.CreatedAt),
        };

        // Paging
        var items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Roles.Where(r => r.DeletedAt == null && EF.Functions.ILike(r.Name, name));
        
        if (excludeId.HasValue)
            dbQuery = dbQuery.Where(r => r.Id != excludeId.Value);
            
        return await dbQuery.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasUsersAsync(int roleId, CancellationToken cancellationToken = default)
        => await _context.Users.AnyAsync(u => u.RoleId == roleId && u.DeletedAt == null, cancellationToken);

    public async Task<bool> HasPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
        => await _context.RoleMenus.AnyAsync(rm => rm.RoleId == roleId, cancellationToken);

    public async Task AddAsync(Role entity, CancellationToken cancellationToken = default)
        => await _context.Roles.AddAsync(entity, cancellationToken);

    public void Update(Role entity)
        => _context.Roles.Update(entity);

    public void Delete(Role entity)
    {
        // Interceptor will handle soft delete
        _context.Roles.Remove(entity);
    }
}
