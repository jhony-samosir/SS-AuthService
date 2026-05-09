using System;

namespace SS.AuthService.Application.Roles.DTOs;

public record RoleDto(
    Guid PublicId,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RoleListItemDto(
    Guid PublicId,
    string Name,
    string? Description,
    DateTime CreatedAt
);
