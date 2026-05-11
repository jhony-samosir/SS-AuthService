using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Common.Constants;

namespace SS.AuthService.Infrastructure.Authentication;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRoleMenuRepository _roleMenuRepository;

    public PermissionHandler(IRoleMenuRepository roleMenuRepository)
    {
        _roleMenuRepository = roleMenuRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        // 1. Ekstrak role_id dari Claims (di-inject oleh Auth Guard/JwtProvider)
        var roleClaim = context.User.FindFirst(ClaimConstants.Role);
        if (roleClaim == null || !int.TryParse(roleClaim.Value, out var roleId))
        {
            return; // Forbidden
        }

        // 2. Cek ke database (RBAC)
        var hasPermission = await _roleMenuRepository.HasPermissionAsync(
            roleId, 
            requirement.Menu, 
            requirement.Action);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
