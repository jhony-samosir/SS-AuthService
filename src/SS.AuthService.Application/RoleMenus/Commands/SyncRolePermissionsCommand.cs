using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.RoleMenus.DTOs;

namespace SS.AuthService.Application.RoleMenus.Commands;

public record SyncRolePermissionsCommand(
    Guid RolePublicId,
    List<SyncRolePermissionInput> Permissions
) : IRequest<Result<bool>>;
