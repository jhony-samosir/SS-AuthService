using System;

namespace SS.AuthService.Application.RoleMenus.DTOs;

public record RolePermissionDto(
    Guid MenuPublicId,
    string MenuName,
    string MenuPath,
    bool CanCreate,
    bool CanRead,
    bool CanUpdate,
    bool CanDelete
);

public record SyncRolePermissionInput(
    Guid MenuId,
    bool CanCreate,
    bool CanRead,
    bool CanUpdate,
    bool CanDelete
);
