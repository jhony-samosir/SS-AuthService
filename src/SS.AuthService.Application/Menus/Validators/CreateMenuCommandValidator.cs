using FluentValidation;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Menus.Commands;

namespace SS.AuthService.Application.Menus.Validators;

public class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
{
    private readonly IMenuRepository _menuRepository;

    public CreateMenuCommandValidator(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Path)
            .NotEmpty().MaximumLength(255)
            .MustAsync(async (path, cancellationToken) => 
                !await _menuRepository.ExistsByPathAsync(path, null, cancellationToken))
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
    }
}
