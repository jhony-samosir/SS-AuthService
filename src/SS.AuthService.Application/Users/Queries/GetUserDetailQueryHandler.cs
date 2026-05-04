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

        return new UserProfileDto(
            user.PublicId,
            user.Email,
            user.FullName,
            user.Role?.Name ?? "Unknown",
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
