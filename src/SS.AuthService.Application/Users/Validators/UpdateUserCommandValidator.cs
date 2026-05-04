using FluentValidation;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("Public ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Valid Role ID is required.");
    }
}
