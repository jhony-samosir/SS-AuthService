using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SS.AuthService.API.DTOs;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Users.Queries;
using SS.AuthService.Domain.Constants;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SS.AuthService.Application.Common.Settings.SecuritySettings _securitySettings;

    public AuthController(IMediator mediator, Microsoft.Extensions.Options.IOptions<SS.AuthService.Application.Common.Settings.SecuritySettings> securitySettings)
    {
        _mediator = mediator;
        _securitySettings = securitySettings.Value;
    }

    [HttpPost("register")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }

        return BadRequest(new { message = result.Message });
    }

    [HttpPost("login")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginUserCommand(request.Email, request.Password, request.DeviceInfo);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        // Set Refresh Token in HttpOnly Cookie for security
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Always use HTTPS in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_securitySettings.RefreshTokenExpiryDays)
        };
        Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpGet("verify-email")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        var command = new VerifyEmailCommand(token);
        var result = await _mediator.Send(command);

        return result switch
        {
            VerifyEmailResult.Success => Ok(new { message = "Email verified successfully. You can now login." }),
            VerifyEmailResult.TokenExpired => BadRequest(new { message = "Verification link has expired. Please request a new one." }),
            VerifyEmailResult.TokenNotFound => BadRequest(new { message = "Invalid verification link." }),
            _ => BadRequest(new { message = "An error occurred during verification." })
        };
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        await _mediator.Send(command);

        // Selalu return sukses untuk mencegah Account Enumeration
        return Ok(new { message = "If your email is registered, a password reset link has been sent to your inbox." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Password has been reset successfully. All active sessions have been revoked." });
        }

        // Handle specific error codes for better UX
        return result.ErrorCode switch
        {
            "PasswordUsedRecently" => BadRequest(new { message = result.ErrorMessage }),
            "InvalidToken" => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = "An error occurred during password reset." })
        };
    }
}
