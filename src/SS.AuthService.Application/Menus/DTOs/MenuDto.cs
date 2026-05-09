using System;

namespace SS.AuthService.Application.Menus.DTOs;

public record MenuDto(
    Guid PublicId,
    Guid? ParentPublicId,
    string Name,
    string Path,
    string? Icon,
    short SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
