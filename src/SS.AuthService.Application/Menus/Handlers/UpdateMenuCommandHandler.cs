using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.Commands;
using SS.AuthService.Application.Menus.DTOs;

namespace SS.AuthService.Application.Menus.Handlers;

public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand, MenuDto>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuCommandHandler(IMenuRepository menuRepository, IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuDto> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw new KeyNotFoundException($"Menu with PublicId {request.PublicId} not found.");

        int? parentId = null;
        if (request.ParentPublicId.HasValue)
        {
            var parent = await _menuRepository.GetByPublicIdAsync(request.ParentPublicId.Value, cancellationToken);
            parentId = parent?.Id;
        }

        menu.Name = request.Name;
        menu.Path = request.Path;
        menu.Icon = request.Icon;
        menu.SortOrder = request.SortOrder;
        menu.ParentId = parentId;

        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MenuDto(
            menu.PublicId,
            request.ParentPublicId,
            menu.Name,
            menu.Path,
            menu.Icon,
            menu.SortOrder,
            menu.CreatedAt,
            menu.UpdatedAt
        );
    }
}
