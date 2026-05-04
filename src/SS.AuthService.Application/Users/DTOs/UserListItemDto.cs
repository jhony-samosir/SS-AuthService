using System;

namespace SS.AuthService.Application.Users.DTOs;

public record UserListItemDto(
    Guid PublicId,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    bool MfaEnabled,
    bool IsLocked,
    DateTime? EmailVerifiedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
