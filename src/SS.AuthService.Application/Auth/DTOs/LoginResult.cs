namespace SS.AuthService.Application.Auth.DTOs;

public record LoginResult(
    bool Success, 
    string Message, 
    string? AccessToken = null, 
    string? RefreshToken = null,
    int StatusCode = 200,
    bool IsMfaRequired = false);
