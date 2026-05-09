using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _context;

    public MenuRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Menu?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Menus
            .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null, cancellationToken);

    public async Task<Menu?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => await _context.Menus
            .Include(m => m.Parent)
            .FirstOrDefaultAsync(m => m.PublicId == publicId && m.DeletedAt == null, cancellationToken);

    public async Task<List<Menu>> GetByPublicIdsAsync(IEnumerable<Guid> publicIds, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .Where(m => publicIds.Contains(m.PublicId) && m.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Menus.AsNoTracking();
        
        if (!includeDeleted)
            query = query.Where(m => m.DeletedAt == null);
            
        return await query
            .OrderBy(m => m.ParentId)
            .ThenBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Menu>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        // Load all active menus into memory and manually link them to build the tree.
        // This is necessary because AsNoTracking() doesn't perform relationship fix-up.
        var allMenus = await _context.Menus
            .AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        var menuMap = allMenus.ToDictionary(m => m.Id);
        foreach (var menu in allMenus)
        {
            if (menu.ParentId.HasValue && menuMap.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(menu);
            }
        }
            
        return allMenus.Where(m => m.ParentId == null).ToList();
    }

    public async Task<bool> ExistsByPathAsync(string path, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Menus.Where(m => m.Path == path && m.DeletedAt == null);
        
        if (excludeId.HasValue)
            query = query.Where(m => m.Id != excludeId.Value);
            
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Menu entity, CancellationToken cancellationToken = default)
        => await _context.Menus.AddAsync(entity, cancellationToken);

    public void Update(Menu entity)
        => _context.Menus.Update(entity);

    public void Delete(Menu entity)
    {
        // AuditInterceptor will catch this 'Deleted' state and convert it 
        // to a soft-delete (setting DeletedAt/DeletedBy).
        _context.Menus.Remove(entity);
    }
}
