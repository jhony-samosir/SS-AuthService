using System.Collections.Generic;

namespace SS.AuthService.Application.RoleMenus.DTOs;

public class SyncRolePermissionsRequest
{
    public List<SyncRolePermissionInput> Permissions { get; set; } = new();
}
