using MediatR;
using Microsoft.AspNetCore.Mvc;
using SS.AuthService.Application.Security.Queries;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly IMediator _mediator;

    public SecurityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Mengambil daftar percobaan login untuk audit keamanan.
    /// </summary>
    [HttpGet("login-attempts")]
    [AuthorizePermission("SecurityAudit", "Read")]
    public async Task<IActionResult> GetLoginAttempts(
        [FromQuery] string? email,
        [FromQuery] Guid? userPublicId,
        [FromQuery] string? ipAddress,
        [FromQuery] bool? isSuccess,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetLoginAttemptsQuery(email, userPublicId, ipAddress, isSuccess, fromDate, toDate, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail percobaan login spesifik.
    /// </summary>
    [HttpGet("login-attempts/{id:long}")]
    [AuthorizePermission("SecurityAudit", "Read")]
    public async Task<IActionResult> GetLoginAttempt(long id)
    {
        var result = await _mediator.Send(new GetLoginAttemptByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }
}
