namespace SS.AuthService.Application.Interfaces;

public interface IRoleMenuRepository
{
    /// <summary>
    /// Memeriksa apakah role tertentu memiliki izin aksi spesifik pada menu tertentu.
    /// Menggunakan query join antara role_menu dan menus.
    /// </summary>
    Task<bool> HasPermissionAsync(int roleId, string menuPath, string action, CancellationToken ct = default);
}
