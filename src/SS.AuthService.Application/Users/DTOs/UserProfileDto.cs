namespace SS.AuthService.Application.Users.DTOs;

public record UserProfileDto(
    Guid PublicId,
    string Email,
    string FullName,
    bool IsVerified
);
