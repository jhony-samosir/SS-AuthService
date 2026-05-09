using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.DTOs;
using SS.AuthService.Application.Roles.Queries;

namespace SS.AuthService.Application.Roles.Handlers;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto?>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (role == null) return null;

        return new RoleDto(
            role.PublicId,
            role.Name,
            role.Description,
            role.CreatedAt,
            role.UpdatedAt
        );
    }
}
