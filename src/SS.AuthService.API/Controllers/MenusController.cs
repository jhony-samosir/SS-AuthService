using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SS.AuthService.Application.Menus.Commands;
using SS.AuthService.Application.Menus.Queries;
using SS.AuthService.Infrastructure.Authentication;

namespace SS.AuthService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenusController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AuthorizePermission("MenuManagement", "Read")]
    public async Task<IActionResult> GetFlat()
    {
        var result = await _mediator.Send(new GetMenusQuery());
        return Ok(result);
    }

    [HttpGet("tree")]
    [AuthorizePermission("Menus", "Read")]
    public async Task<IActionResult> GetTree()
    {
        var result = await _mediator.Send(new GetMenuTreeQuery());
        return Ok(result);
    }

    [HttpGet("{publicId:guid}")]
    [AuthorizePermission("MenuManagement", "Read")]
    public async Task<IActionResult> GetById(Guid publicId)
    {
        var result = await _mediator.Send(new GetMenuByIdQuery(publicId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [AuthorizePermission("MenuManagement", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateMenuCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { publicId = result.PublicId }, result);
    }

    [HttpPut("{publicId:guid}")]
    [AuthorizePermission("MenuManagement", "Update")]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateMenuCommand command)
    {
        if (publicId != command.PublicId) return BadRequest("PublicId mismatch.");
        
        try 
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{publicId:guid}")]
    [AuthorizePermission("MenuManagement", "Delete")]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        var result = await _mediator.Send(new DeleteMenuCommand(publicId));
        if (!result) return NotFound();
        return NoContent();
    }
}
