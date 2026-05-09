namespace SS.AuthService.Application.Users.DTOs;

/// <summary>Request body untuk endpoint Lock User.</summary>
public record LockUserRequest(DateTime? LockedUntil, int? LockDurationMinutes);
