using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.DTOs;
using SS.AuthService.Application.Menus.Queries;

namespace SS.AuthService.Application.Menus.Handlers;

public class GetMenusQueryHandler : IRequestHandler<GetMenusQuery, List<MenuDto>>
{
    private readonly IMenuRepository _menuRepository;

    public GetMenusQueryHandler(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<List<MenuDto>> Handle(GetMenusQuery request, CancellationToken cancellationToken)
    {
        var menus = await _menuRepository.GetAllAsync(false, cancellationToken);
        
        return menus.Select(m => new MenuDto(
            m.PublicId,
            m.Parent?.PublicId,
            m.Name,
            m.Path,
            m.Icon,
            m.SortOrder,
            m.CreatedAt,
            m.UpdatedAt
        )).ToList();
    }
}
