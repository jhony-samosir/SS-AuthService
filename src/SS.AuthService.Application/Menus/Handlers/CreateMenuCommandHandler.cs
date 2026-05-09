using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.Commands;
using SS.AuthService.Application.Menus.DTOs;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Menus.Handlers;

public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, MenuDto>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMenuCommandHandler(IMenuRepository menuRepository, IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuDto> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        int? parentId = null;
        if (request.ParentPublicId.HasValue)
        {
            var parent = await _menuRepository.GetByPublicIdAsync(request.ParentPublicId.Value, cancellationToken);
            parentId = parent?.Id;
        }

        var menu = new Menu
        {
            PublicId = Guid.NewGuid(),
            Name = request.Name,
            Path = request.Path,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            ParentId = parentId
        };

        await _menuRepository.AddAsync(menu, cancellationToken);
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
