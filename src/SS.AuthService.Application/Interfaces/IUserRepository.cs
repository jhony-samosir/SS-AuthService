using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IUserRepository
{
    /// <summary>Cek apakah email sudah terdaftar (case-insensitive). Aman dari Account Enumeration.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRoleAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>Tambah user baru ke context (belum SaveChanges).</summary>
    Task AddAsync(User entity, CancellationToken cancellationToken = default);

    void Update(User entity);

    /// <summary>Ambil ID role default "Customer" dari database. Mencegah magic number hardcoded.</summary>
    Task<int> GetDefaultCustomerRoleIdAsync(CancellationToken cancellationToken = default);

    /// <summary>List dengan filter + sort + pagination.</summary>
    Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        SS.AuthService.Application.Users.Queries.UserFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Cek apakah RoleId valid.</summary>
    Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>Hard delete (untuk cleanup atau jika benar-benar dibutuhkan).</summary>
    void Delete(User entity);

    /// <summary>Bulk fetch users by their internal IDs.</summary>
    Task<List<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
