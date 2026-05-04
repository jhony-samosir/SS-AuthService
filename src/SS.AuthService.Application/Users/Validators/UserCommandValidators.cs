using FluentValidation;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Validators;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator() { RuleFor(x => x.PublicId).NotEmpty(); }
}

public class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator() { RuleFor(x => x.PublicId).NotEmpty(); }
}

public class ForcePasswordResetCommandValidator : AbstractValidator<ForcePasswordResetCommand>
{
    public ForcePasswordResetCommandValidator() { RuleFor(x => x.PublicId).NotEmpty(); }
}
