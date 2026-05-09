using System;

namespace SS.AuthService.Application.Users.DTOs;

public record UserSessionDto(
    Guid PublicId,
    string? DeviceInfo,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsRevoked
)
{
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}
