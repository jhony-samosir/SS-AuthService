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

    /// <summary>
    /// Mengaktifkan user.
    /// </summary>
    [HttpPut("{publicId:guid}/activate")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> Activate(Guid publicId)
    {
        var result = await _mediator.Send(new ActivateUserCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }

    /// <summary>
    /// Menonaktifkan user (deactivate).
    /// </summary>
    [HttpPut("{publicId:guid}/deactivate")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> Deactivate(Guid publicId)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }

    /// <summary>
    /// Mengunci user (lock).
    /// </summary>
    [HttpPut("{publicId:guid}/lock")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> Lock(Guid publicId, [FromBody] LockUserRequest request)
    {
        var result = await _mediator.Send(new LockUserCommand(publicId, request.LockedUntil, request.LockDurationMinutes));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }

    /// <summary>
    /// Mengubah role user.
    /// </summary>
    [HttpPut("{publicId:guid}/role")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> AssignRole(Guid publicId, [FromBody] AssignRoleRequest request)
    {
        var result = await _mediator.Send(new AssignUserRoleCommand(publicId, request.RolePublicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "UserNotFound" or "RoleNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }
}

public record LockUserRequest(DateTime? LockedUntil, int? LockDurationMinutes);
public record AssignRoleRequest(Guid RolePublicId);
