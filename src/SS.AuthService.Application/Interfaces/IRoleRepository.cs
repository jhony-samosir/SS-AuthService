using SS.AuthService.Domain.Entities;
using SS.AuthService.Application.Roles.Queries;

namespace SS.AuthService.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Role?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    
    Task<(List<Role> Items, int TotalCount)> GetPagedAsync(GetRolesQuery query, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    
    Task<bool> HasUsersAsync(int roleId, CancellationToken cancellationToken = default);
    
    Task<bool> HasPermissionsAsync(int roleId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Role entity, CancellationToken cancellationToken = default);
    
    void Update(Role entity);
    
    void Delete(Role entity);
}
