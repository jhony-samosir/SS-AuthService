namespace SS.AuthService.Application.Users.DTOs;

/// <summary>DTO status MFA user untuk keperluan Admin monitoring.</summary>
public record UserMfaInfoDto(bool IsEnabled, int RecoveryCodesRemaining);
