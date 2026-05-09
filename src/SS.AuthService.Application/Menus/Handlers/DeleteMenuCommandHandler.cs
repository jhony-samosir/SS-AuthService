using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.Commands;

namespace SS.AuthService.Application.Menus.Handlers;

public class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand, bool>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMenuCommandHandler(IMenuRepository menuRepository, IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (menu == null) return false;

        _menuRepository.Delete(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
