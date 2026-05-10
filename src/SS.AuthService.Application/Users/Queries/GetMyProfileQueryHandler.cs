using MediatR;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProfileQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // Fetch permissions for the role (Cached)
        var permissions = await _unitOfWork.RoleMenus.GetPermissionsByRoleIdAsync(user.RoleId, cancellationToken);

        return new UserProfileDto(
            user.PublicId,
            user.Email,
            user.FullName,
            user.Role != null 
                ? new UserRoleDto(user.Role.PublicId, user.Role.Name) 
                : new UserRoleDto(Guid.Empty, "Unknown"),
            permissions,
            user.IsActive,
            user.MfaEnabled,
            user.EmailVerifiedAt != null,
            user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow,
            user.LockedUntil,
            user.FailedLoginAttempts,
            user.TosAcceptedAt,
            user.PrivacyPolicyAcceptedAt,
            user.CreatedAt,
            user.CreatedBy,
            user.UpdatedAt,
            user.UpdatedBy
        );
    }
}
