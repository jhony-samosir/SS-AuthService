using FluentValidation;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Validators;

public class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty();
        RuleFor(x => x.RolePublicId).NotEmpty();
    }
}
