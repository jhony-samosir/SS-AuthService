using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class RoleMenuRepository : IRoleMenuRepository
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RoleMenuRepository> _logger;

    public RoleMenuRepository(AppDbContext context, IMemoryCache cache, ILogger<RoleMenuRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int roleId, string menuPath, string action, CancellationToken ct = default)
    {
        // Use versioning to support wildcard-like invalidation for a specific role
        int version = _cache.GetOrCreate($"ver:role:{roleId}", _ => 0);
        string cacheKey = $"perm:{roleId}:{version}:{menuPath}:{action}";

        if (_cache.TryGetValue(cacheKey, out bool hasPermission))
        {
            return hasPermission;
        }

        _logger.LogInformation("Cache miss for permission {CacheKey}. Querying database.", cacheKey);

        var query = from rm in _context.RoleMenus
                    join m in _context.Menus on rm.MenuId equals m.Id
                    where rm.RoleId == roleId 
                          && m.Path == menuPath 
                          && m.DeletedAt == null 
                          && rm.DeletedAt == null
                    select rm;

        var permission = await query.FirstOrDefaultAsync(ct);

        bool result = false;
        if (permission != null)
        {
            result = action.ToLower() switch
            {
                "create" => permission.CanCreate,
                "read" => permission.CanRead,
                "update" => permission.CanUpdate,
                "delete" => permission.CanDelete,
                _ => false
            };
        }

        // Cache hasil selama 15 menit
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        
        return result;
    }

    public async Task<List<SS.AuthService.Domain.Entities.RoleMenu>> GetByRoleIdAsync(int roleId, CancellationToken ct = default)
    {
        return await _context.RoleMenus
            .Include(rm => rm.Menu)
            .Where(rm => rm.RoleId == roleId && rm.DeletedAt == null)
            .ToListAsync(ct);
    }
    
    public async Task<List<string>> GetPermissionsByRoleIdAsync(int roleId, CancellationToken ct = default)
    {
        // Use versioning to support wildcard-like invalidation for a specific role
        int version = _cache.GetOrCreate($"ver:role:{roleId}", _ => 0);
        string cacheKey = $"perm_list:{roleId}:{version}";

        if (_cache.TryGetValue(cacheKey, out List<string>? cachedPermissions))
        {
            return cachedPermissions ?? new List<string>();
        }

        _logger.LogInformation("Cache miss for permission list of RoleId: {RoleId}. Querying database.", roleId);

        var roleMenus = await _context.RoleMenus
            .AsNoTracking()
            .Include(rm => rm.Menu)
            .Where(rm => rm.RoleId == roleId && rm.DeletedAt == null)
            .ToListAsync(ct);

        var permissions = new List<string>();
        foreach (var rm in roleMenus)
        {
            // Standardize: Add base menu name if any permission is granted
            if (rm.CanRead || rm.CanCreate || rm.CanUpdate || rm.CanDelete)
            {
                permissions.Add(rm.Menu.Name);
            }

            // Standardize: Use SPACE as separator to match frontend ADMIN_PERMISSIONS constants
            if (rm.CanRead) permissions.Add($"{rm.Menu.Name} Read");
            if (rm.CanCreate) permissions.Add($"{rm.Menu.Name} Create");
            if (rm.CanUpdate) permissions.Add($"{rm.Menu.Name} Update");
            if (rm.CanDelete) permissions.Add($"{rm.Menu.Name} Delete");
        }

        // Cache hasil selama 15 menit
        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(15));
        
        return permissions;
    }

    public void Add(SS.AuthService.Domain.Entities.RoleMenu entity)
    {
        _context.RoleMenus.Add(entity);
    }

    public void RemoveRange(IEnumerable<SS.AuthService.Domain.Entities.RoleMenu> entities)
    {
        _context.RoleMenus.RemoveRange(entities);
    }

    public void InvalidateCache(int roleId)
    {
        _logger.LogInformation("Invalidating permission cache for RoleId: {RoleId} by incrementing version.", roleId);
        int version = _cache.GetOrCreate($"ver:role:{roleId}", _ => 0);
        _cache.Set($"ver:role:{roleId}", version + 1);
    }
}
