using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Application.Users.Queries;
using SS.AuthService.Infrastructure.Authentication;
using System;
using System.Threading.Tasks;

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
    /// Mengambil profil user yang sedang login.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _mediator.Send(new GetMyProfileQuery());
        if (result == null)
        {
            return Unauthorized();
        }
        return Ok(result);
    }

    /// <summary>
    /// Mengambil daftar user dengan filter, sort, dan pagination.
    /// </summary>
    [HttpGet]
    [AuthorizePermission("UserManagement", "Read")]
    public async Task<IActionResult> GetList([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail user berdasarkan PublicId.
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [AuthorizePermission("UserManagement", "Read")]
    public async Task<IActionResult> GetDetail(Guid publicId)
    {
        var result = await _mediator.Send(new GetUserDetailQuery(publicId));
        if (result == null)
        {
            return NotFound(new { message = "User not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Membuat user baru oleh Admin.
    /// </summary>
    [HttpPost]
    [AuthorizePermission("UserManagement", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "EmailAlreadyExists")
                return Conflict(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetDetail), new { publicId = result.Value }, new { publicId = result.Value });
    }

    /// <summary>
    /// Memperbarui data user.
    /// </summary>
    [HttpPut("{publicId:guid}")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateUserCommand command)
    {
        if (publicId != command.PublicId)
        {
            return BadRequest(new { message = "PublicId mismatch." });
        }

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Menghapus user (soft-delete).
    /// </summary>
    [HttpDelete("{publicId:guid}")]
    [AuthorizePermission("UserManagement", "Delete")]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        var result = await _mediator.Send(new DeleteUserCommand(publicId));
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Membuka blokir user (unlock).
    /// </summary>
    [HttpPut("{publicId:guid}/unlock")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> Unlock(Guid publicId)
    {
        var result = await _mediator.Send(new UnlockUserCommand(publicId));
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Memaksa reset password user (kirim email).
    /// </summary>
    [HttpPut("{publicId:guid}/force-reset-password")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> ForceResetPassword(Guid publicId)
    {
        var result = await _mediator.Send(new ForcePasswordResetCommand(publicId));
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Password reset email has been sent to the user." });
    }
}
