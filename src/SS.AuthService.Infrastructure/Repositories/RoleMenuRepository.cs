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
