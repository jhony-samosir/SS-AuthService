using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Menu?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    
    Task<List<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    
    Task<List<Menu>> GetTreeAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByPathAsync(string path, int? excludeId = null, CancellationToken cancellationToken = default);
    
    Task AddAsync(Menu entity, CancellationToken cancellationToken = default);
    
    void Update(Menu entity);
    
    void Delete(Menu entity);
}
