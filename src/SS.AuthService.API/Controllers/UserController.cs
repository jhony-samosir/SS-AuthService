using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SS.AuthService.Application.Users.Queries;
using SS.AuthService.Domain.Constants;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Mengambil profil user berdasarkan PublicId (UUID).
    /// Business logic dipindah ke Application Layer (Thin Controller).
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [AuthorizePermission("UserManagement", "Read")]
    public async Task<IActionResult> GetProfile(Guid publicId)
    {
        var loggedInUserIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (loggedInUserIdClaim == null || !int.TryParse(loggedInUserIdClaim.Value, out var loggedInUserId))
        {
            return Unauthorized();
        }

        // Gunakan konstanta dari Domain Layer untuk menghindari magic string "1"
        var isAdmin = User.IsInRole(RoleConstants.AdminRoleName);

        var query = new GetProfileQuery(publicId, loggedInUserId, isAdmin);
        var profile = await _mediator.Send(query);

        if (profile == null)
        {
            // Bisa 404 (tidak ditemukan/masking) atau 403 tergantung kebijakan
            return NotFound(new { message = "User not found or access denied." });
        }

        return Ok(profile);
    }
}
