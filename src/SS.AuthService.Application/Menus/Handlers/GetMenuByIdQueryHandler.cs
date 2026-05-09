using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.DTOs;
using SS.AuthService.Application.Menus.Queries;

namespace SS.AuthService.Application.Menus.Handlers;

public class GetMenuByIdQueryHandler : IRequestHandler<GetMenuByIdQuery, MenuDto?>
{
    private readonly IMenuRepository _menuRepository;

    public GetMenuByIdQueryHandler(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<MenuDto?> Handle(GetMenuByIdQuery request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (menu == null) return null;

        return new MenuDto(
            menu.PublicId,
            menu.Parent?.PublicId,
            menu.Name,
            menu.Path,
            menu.Icon,
            menu.SortOrder,
            menu.CreatedAt,
            menu.UpdatedAt
        );
    }
}
