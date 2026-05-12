using System;
using System.Collections.Generic;

namespace SS.AuthService.Application.Users.DTOs;

public record UserRoleDto(Guid PublicId, string Name);

public record UserProfileDto(
    Guid PublicId,
    string Email,
    string FullName,
    UserRoleDto Role,
    List<string> Permissions,
    bool IsActive,
    bool MfaEnabled,
    bool IsEmailVerified,
    bool IsLocked,
    DateTime? LockedUntil,
    short FailedLoginAttempts,
    DateTime? TosAcceptedAt,
    DateTime? PrivacyPolicyAcceptedAt,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime UpdatedAt,
    string? UpdatedBy
);
