using System;

namespace SS.AuthService.Application.Security.DTOs;

public record LoginAttemptDto(
    long Id,
    Guid? UserPublicId,
    string EmailAttempted,
    string IpAddress,
    string? DeviceInfo,
    bool IsSuccess,
    DateTime AttemptedAt
);
