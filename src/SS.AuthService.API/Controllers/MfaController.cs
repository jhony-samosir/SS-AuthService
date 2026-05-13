using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SS.AuthService.API.DTOs;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Common.Settings;
using System;
using SS.AuthService.Application.Common.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using SS.AuthService.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MfaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SecuritySettings _securitySettings;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _env;

    public MfaController(
        IMediator mediator,
        IOptions<SecuritySettings> securitySettings,
        IOptions<JwtOptions> jwtOptions,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _securitySettings = securitySettings.Value;
        _jwtOptions = jwtOptions.Value;
        _env = env;
    }

    [HttpPost("setup")]
    [Authorize]
    public async Task<IActionResult> Setup()
    {
        var userIdString = User.FindFirstValue(ClaimConstants.UserId) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdString, out var userId)) return Unauthorized();

        var command = new SetupMfaCommand(userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("enable")]
    [Authorize]
    public async Task<IActionResult> Enable([FromBody] MfaRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimConstants.UserId) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdString, out var userId)) return Unauthorized();

        var command = new EnableMfaCommand(userId, request.Code);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("verify")]
    [EnableRateLimiting("AuthLimiter")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromBody] MfaVerifyRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var deviceInfo = Request.Headers["User-Agent"].ToString();

        var command = new VerifyMfaCommand(request.MfaToken, request.Code, ipAddress, deviceInfo);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        SetTokenCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new { accessToken = result.AccessToken });
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
