using FluentValidation;
using SS.AuthService.Application.Users.Queries;
using System.Linq;

namespace SS.AuthService.Application.Users.Validators;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    private readonly string[] _validSortColumns = { "email", "fullname", "rolename", "isactive", "mfaenabled", "createdat", "updatedat" };

    public GetUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortDirection)
            .Must(x => x.ToLower() == "asc" || x.ToLower() == "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");

        RuleFor(x => x.SortBy)
            .Must(x => _validSortColumns.Contains(x.ToLower()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", _validSortColumns)}");
            
        RuleFor(x => x.Email)
            .MaximumLength(255).When(x => x.Email != null);
            
        RuleFor(x => x.FullName)
            .MaximumLength(100).When(x => x.FullName != null);
            
        RuleFor(x => x.CreatedAtTo)
            .GreaterThanOrEqualTo(x => x.CreatedAtFrom!.Value)
            .When(x => x.CreatedAtFrom.HasValue && x.CreatedAtTo.HasValue)
            .WithMessage("CreatedAtTo must be greater than or equal to CreatedAtFrom.");
    }
}
