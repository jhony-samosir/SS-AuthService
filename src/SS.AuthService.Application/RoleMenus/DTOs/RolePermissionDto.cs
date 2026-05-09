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
    Guid MenuPublicId,
    bool CanCreate,
    bool CanRead,
    bool CanUpdate,
    bool CanDelete
);
