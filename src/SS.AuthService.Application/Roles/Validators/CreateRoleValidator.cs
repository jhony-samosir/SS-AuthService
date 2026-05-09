using FluentValidation;
using SS.AuthService.Application.Roles.Commands;

namespace SS.AuthService.Application.Roles.Validators;

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name cannot be only whitespace.");

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}
