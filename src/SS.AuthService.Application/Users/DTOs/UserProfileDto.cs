using System;

namespace SS.AuthService.Application.Users.DTOs;

public record UserProfileDto(
    Guid PublicId,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    bool MfaEnabled,
    bool IsEmailVerified,
    bool IsLocked,
    DateTime? LockedUntil,
    short FailedLoginAttempts,
    DateTime? TosAcceptedAt,
    DateTime? PrivacyPolicyAcceptedAt,
    DateTime CreatedAt,
    int? CreatedBy,
    DateTime UpdatedAt,
    int? UpdatedBy
);
