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

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MfaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SecuritySettings _securitySettings;

    public MfaController(
        IMediator mediator,
        IOptions<SecuritySettings> securitySettings)
    {
        _mediator = mediator;
        _securitySettings = securitySettings.Value;
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

        // Set Refresh Token in HttpOnly Cookie (similar to Login)
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_securitySettings.RefreshTokenExpiryDays)
        };
        Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

        return Ok(new { accessToken = result.AccessToken });
    }
}
