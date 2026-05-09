using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    /// <summary>
    /// Mengambil daftar sesi aktif user.
    /// </summary>
    [HttpGet("{publicId:guid}/sessions")]
    [AuthorizePermission("UserManagement", "Read")]
    public async Task<IActionResult> GetSessions(Guid publicId)
    {
        var result = await _mediator.Send(new GetUserSessionsQuery(publicId));
        if (result == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Mencabut semua sesi user.
    /// </summary>
    [HttpDelete("{publicId:guid}/sessions")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> RevokeAllSessions(Guid publicId)
    {
        var result = await _mediator.Send(new RevokeAllUserSessionsCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }

    /// <summary>
    /// Mencabut satu sesi spesifik user.
    /// </summary>
    [HttpDelete("{publicId:guid}/sessions/{sessionPublicId:guid}")]
    [AuthorizePermission("UserManagement", "Update")]
    public async Task<IActionResult> RevokeSession(Guid publicId, Guid sessionPublicId)
    {
        var result = await _mediator.Send(new RevokeUserSessionCommand(publicId, sessionPublicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "UserNotFound" or "SessionNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return NoContent();
    }

    /// <summary>
    /// Mengirim ulang email verifikasi ke user.
    /// </summary>
    [HttpPost("{publicId:guid}/resend-verification")]
    [AuthorizePermission("UserManagement", "Update")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> ResendVerification(Guid publicId)
    {
        var result = await _mediator.Send(new ResendVerificationEmailCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            if (result.ErrorCode == "UserAlreadyVerified") return Conflict(new { message = result.ErrorMessage, code = result.ErrorCode });
            if (result.ErrorCode == "ThrottlingActive") return StatusCode(429, new { message = result.ErrorMessage, code = result.ErrorCode });
            if (result.ErrorCode == "UserInactive") return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return Ok(new { message = "Verification email has been resent." });
    }

    /// <summary>
    /// Menonaktifkan MFA untuk user (Audit/Recovery).
    /// </summary>
    [HttpPut("{publicId:guid}/mfa/disable")]
    [AuthorizePermission("UserManagement", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableMfa(Guid publicId)
    {
        var result = await _mediator.Send(new DisableUserMfaCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return Ok(new { message = "MFA has been disabled for this user." });
    }

    /// <summary>
    /// Membuat ulang kode pemulihan MFA untuk user. Kode baru akan dikirimkan langsung ke email user demi keamanan.
    /// </summary>
    [HttpPost("{publicId:guid}/mfa/recovery-codes/regenerate")]
    [AuthorizePermission("UserManagement", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateRecoveryCodes(Guid publicId)
    {
        var result = await _mediator.Send(new RegenerateRecoveryCodesCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return Ok(new { message = "MFA recovery codes have been regenerated and sent to the user's email securely." });
    }

    /// <summary>
    /// Mengambil info status MFA user.
    /// </summary>
    [HttpGet("{publicId:guid}/mfa")]
    [AuthorizePermission("UserManagement", "Read")]
    public async Task<IActionResult> GetMfaInfo(Guid publicId)
    {
        var result = await _mediator.Send(new GetUserMfaInfoQuery(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "UserNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }
}

public record LockUserRequest(DateTime? LockedUntil, int? LockDurationMinutes);
public record AssignRoleRequest(Guid RolePublicId);
