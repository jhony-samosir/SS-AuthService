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
        string cacheKey = $"perm:{roleId}:{menuPath}:{action}";

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

        // Cache hasil selama 15 menit untuk mengurangi beban DB
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        
        return result;
    }
}
