using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SS.AuthService.Application.Roles.Commands;
using SS.AuthService.Application.Roles.Queries;
using SS.AuthService.Application.RoleMenus.Commands;
using SS.AuthService.Application.RoleMenus.Queries;
using SS.AuthService.Application.RoleMenus.DTOs;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AuthorizePermission("RoleManagement", "Read")]
    public async Task<IActionResult> GetList([FromQuery] GetRolesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{publicId:guid}")]
    [AuthorizePermission("RoleManagement", "Read")]
    public async Task<IActionResult> GetById(Guid publicId)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(publicId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [AuthorizePermission("RoleManagement", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RoleNameAlreadyExists")
                return Conflict(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { publicId = result.Value }, new { publicId = result.Value });
    }

    [HttpPut("{publicId:guid}")]
    [AuthorizePermission("RoleManagement", "Update")]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateRoleCommand command)
    {
        if (publicId != command.PublicId) return BadRequest("PublicId mismatch.");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RoleNotFound") return NotFound();
            if (result.ErrorCode == "RoleNameAlreadyExists") return Conflict(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    [HttpDelete("{publicId:guid}")]
    [AuthorizePermission("RoleManagement", "Delete")]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(publicId));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RoleNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    [HttpGet("{publicId:guid}/permissions")]
    [AuthorizePermission("RoleManagement", "Read")]
    public async Task<IActionResult> GetPermissions(Guid publicId)
    {
        var result = await _mediator.Send(new GetRolePermissionsQuery(publicId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{publicId:guid}/permissions")]
    [AuthorizePermission("RoleManagement", "Update")]
    public async Task<IActionResult> SyncPermissions(Guid publicId, [FromBody] List<SyncRolePermissionInput> permissions)
    {
        var result = await _mediator.Send(new SyncRolePermissionsCommand(publicId, permissions));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RoleNotFound") return NotFound();
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }
}
