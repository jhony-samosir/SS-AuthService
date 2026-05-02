using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SS.AuthService.API.DTOs;
using SS.AuthService.Application.Auth.Commands;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.FullName,
            request.AcceptTos,
            request.AcceptPrivacyPolicy);

        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }
        
        return BadRequest(new { message = result.Message });
    }
}
