using FluentValidation;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Validators;

public class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty();
        
        RuleFor(x => x)
            .Must(x => x.LockedUntil.HasValue ^ x.LockDurationMinutes.HasValue)
            .WithMessage("Please provide EITHER LockedUntil OR LockDurationMinutes, but not both.");

        RuleFor(x => x.LockedUntil)
            .Must(x => !x.HasValue || x.Value > DateTime.UtcNow)
            .WithMessage("LockedUntil must be in the future.");

        RuleFor(x => x.LockDurationMinutes)
            .Must(x => !x.HasValue || x.Value > 0)
            .WithMessage("LockDurationMinutes must be greater than 0.");
    }
}
