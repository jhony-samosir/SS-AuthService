namespace SS.AuthService.Application.Interfaces;

public interface IRoleMenuRepository
{
    /// <summary>
    /// Memeriksa apakah role tertentu memiliki izin aksi spesifik pada menu tertentu.
    /// Menggunakan query join antara role_menu dan menus.
    /// </summary>
    Task<bool> HasPermissionAsync(int roleId, string menuPath, string action, CancellationToken ct = default);

    Task<List<SS.AuthService.Domain.Entities.RoleMenu>> GetByRoleIdAsync(int roleId, CancellationToken ct = default);

    /// <summary>
    /// Mengambil daftar permission (Menu:Action) untuk role tertentu. Hasilnya di-cache.
    /// </summary>
    Task<List<string>> GetPermissionsByRoleIdAsync(int roleId, CancellationToken ct = default);

    void Add(SS.AuthService.Domain.Entities.RoleMenu entity);

    void RemoveRange(IEnumerable<SS.AuthService.Domain.Entities.RoleMenu> entities);

    void InvalidateCache(int roleId);
}
