using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var filter = new UserFilter(
            request.Email,
            request.FullName,
            request.RoleId,
            request.IsActive,
            request.MfaEnabled,
            request.IsLocked,
            request.CreatedAtFrom,
            request.CreatedAtTo,
            request.SortBy,
            request.SortDirection,
            request.PageNumber,
            request.PageSize
        );

        var (users, totalCount) = await _unitOfWork.Users.GetPagedAsync(filter, cancellationToken);

        var items = users.Select(u => new UserListItemDto(
            u.PublicId,
            u.Email,
            u.FullName,
            u.Role?.Name ?? "Unknown",
            u.IsActive,
            u.MfaEnabled,
            u.LockedUntil.HasValue && u.LockedUntil.Value > System.DateTime.UtcNow,
            u.EmailVerifiedAt,
            u.CreatedAt,
            u.UpdatedAt
        )).ToList();

        return new PagedResult<UserListItemDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
