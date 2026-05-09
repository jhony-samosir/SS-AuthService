using MediatR;
using SS.AuthService.Application.RoleMenus.DTOs;

namespace SS.AuthService.Application.RoleMenus.Queries;

public record GetRolePermissionsQuery(Guid RolePublicId) : IRequest<List<RolePermissionDto>?>;
