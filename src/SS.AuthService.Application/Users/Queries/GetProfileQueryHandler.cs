using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetProfileQueryHandler> _logger;

    public GetProfileQueryHandler(IUnitOfWork unitOfWork, ILogger<GetProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserProfileDto?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. Cari user berdasarkan PublicId
        var targetUser = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        
        if (targetUser == null)
        {
            return null;
        }

        // 2. IDOR Prevention & Admin Audit Logic
        bool isOwner = targetUser.Id == request.LoggedInUserId;

        if (!isOwner && !request.IsAdmin)
        {
            // Lempar exception atau return null (api akan return 403)
            // Di sini kita kembalikan null agar controller handle 404/403
            return null; 
        }

        // 3. Audit Trail (Enterprise Best Practice)
        if (request.IsAdmin && !isOwner)
        {
            _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} accessed profile of User {UserId} ({PublicId})", 
                request.LoggedInUserId, targetUser.Id, targetUser.PublicId);
        }

        return new UserProfileDto(
            targetUser.PublicId,
            targetUser.Email,
            targetUser.FullName,
            targetUser.EmailVerifiedAt != null
        );
    }
}
