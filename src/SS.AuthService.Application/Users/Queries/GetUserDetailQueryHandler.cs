using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Queries;

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        
        if (user == null) return null;

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
