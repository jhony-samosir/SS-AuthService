using FluentValidation;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.Commands;

namespace SS.AuthService.Application.Menus.Validators;

public class UpdateMenuCommandValidator : AbstractValidator<UpdateMenuCommand>
{
    private readonly IMenuRepository _menuRepository;

    public UpdateMenuCommandValidator(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;

        RuleFor(x => x.PublicId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Path)
            .NotEmpty().MaximumLength(255)
            .MustAsync(async (command, path, cancellationToken) => 
            {
                var menu = await _menuRepository.GetByPublicIdAsync(command.PublicId, cancellationToken);
                if (menu == null) return true; // Let the handler handle 404
                return !await _menuRepository.ExistsByPathAsync(path, menu.Id, cancellationToken);
            })
            .WithMessage("Path must be unique.");

        RuleFor(x => x.Icon)
            .MaximumLength(50);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(x => x.ParentPublicId)
            .MustAsync(async (parentId, cancellationToken) => 
            {
                if (!parentId.HasValue) return true;
                var parent = await _menuRepository.GetByPublicIdAsync(parentId.Value, cancellationToken);
                return parent != null;
            })
            .WithMessage("Parent menu not found.");
            
        RuleFor(x => x.ParentPublicId)
            .NotEqual(x => x.PublicId)
            .WithMessage("A menu cannot be its own parent.");
    }
}
