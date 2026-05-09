using FluentValidation;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Validators;

public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty();
    }
}

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty();
    }
}
