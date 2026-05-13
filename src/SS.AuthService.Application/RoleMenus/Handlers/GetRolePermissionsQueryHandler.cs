using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.RoleMenus.DTOs;
using SS.AuthService.Application.RoleMenus.Queries;

namespace SS.AuthService.Application.RoleMenus.Handlers;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<RolePermissionDto>?>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleMenuRepository _roleMenuRepository;

    public GetRolePermissionsQueryHandler(IRoleRepository roleRepository, IRoleMenuRepository roleMenuRepository)
    {
        _roleRepository = roleRepository;
        _roleMenuRepository = roleMenuRepository;
    }

    public async Task<List<RolePermissionDto>?> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByPublicIdAsync(request.RolePublicId, cancellationToken);
        if (role == null) return null;

        var permissions = await _roleMenuRepository.GetByRoleIdAsync(role.Id, cancellationToken);

        return permissions.Select(p => new RolePermissionDto(
            p.Menu.PublicId,
            p.Menu.Name,
            p.Menu.Path,
            p.CanCreate,
            p.CanRead,
            p.CanUpdate,
            p.CanDelete
        )).ToList();
    }
}
