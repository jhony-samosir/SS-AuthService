using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Users.Queries;
using SS.AuthService.Domain.Constants;
using SS.AuthService.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SS.AuthService.Application.Common.Settings.SecuritySettings _securitySettings;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IMediator mediator, 
        Microsoft.Extensions.Options.IOptions<SS.AuthService.Application.Common.Settings.SecuritySettings> securitySettings,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _securitySettings = securitySettings.Value;
        _jwtOptions = jwtOptions.Value;
        _env = env;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthLimiter")]
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
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        if (result.IsMfaRequired || result.StatusCode == 202)
        {
            return Accepted(new 
            { 
                message = result.Message, 
                mfaToken = result.AccessToken,
                isMfaRequired = true 
            });
        }

        SetTokenCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpGet("verify-email")]
    [EnableRateLimiting("AuthLimiter")]
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
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _mediator.Send(command);

        // Selalu return sukses untuk mencegah Account Enumeration
        return Ok(new { message = "If your email is registered, a password reset link has been sent to your inbox." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
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

    [HttpPost("refresh")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var deviceInfo = Request.Headers["User-Agent"].ToString();

        var command = new RefreshTokenCommand(refreshToken, ipAddress, deviceInfo);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        SetTokenCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        Request.Cookies.TryGetValue("refreshToken", out var refreshToken);
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var command = new LogoutCommand(refreshToken);
            await _mediator.Send(command);
        }

        // Clear cookies
        var cookieOptions = GetCookieOptions();

        Response.Cookies.Delete("refreshToken", cookieOptions);
        Response.Cookies.Delete("accessToken", cookieOptions);

        return Ok(new { message = "Logged out successfully." });
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = GetCookieOptions();

        // Access Token Cookie (Short-lived for Gateway/Session recovery)
        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
        });

        // Refresh Token Cookie (Long-lived for Persistence)
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            Expires = DateTime.UtcNow.AddDays(_securitySettings.RefreshTokenExpiryDays)
        });
    }

    private CookieOptions GetCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            // Only use Secure cookies in Production
            Secure = _env.IsProduction(),
            // SameSite=Strict can be problematic in Dev on HTTP
            SameSite = _env.IsProduction() ? SameSiteMode.Strict : SameSiteMode.Lax
        };
    }
}
