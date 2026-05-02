namespace SS.AuthService.API.DTOs;

public record ResetPasswordRequest(string Token, string NewPassword);
