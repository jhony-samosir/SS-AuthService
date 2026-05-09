using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.DTOs;
using SS.AuthService.Application.Menus.Queries;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Menus.Handlers;

public class GetMenuTreeQueryHandler : IRequestHandler<GetMenuTreeQuery, List<MenuTreeDto>>
{
    private readonly IMenuRepository _menuRepository;

    public GetMenuTreeQueryHandler(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<List<MenuTreeDto>> Handle(GetMenuTreeQuery request, CancellationToken cancellationToken)
    {
        var rootMenus = await _menuRepository.GetTreeAsync(cancellationToken);
        
        return rootMenus.Select(MapToDto).ToList();
    }

    private MenuTreeDto MapToDto(Menu menu)
    {
        return new MenuTreeDto(
            menu.PublicId,
            menu.Name,
            menu.Path,
            menu.Icon,
            menu.SortOrder,
            menu.Children
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.SortOrder)
                .Select(MapToDto)
                .ToList()
        );
    }
}
