using MediatR;
using SS.AuthService.Application.Menus.DTOs;

namespace SS.AuthService.Application.Menus.Commands;

public record CreateMenuCommand(
    string Name,
    string Path,
    string? Icon,
    short SortOrder,
    Guid? ParentPublicId
) : IRequest<MenuDto>;
