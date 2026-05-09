using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    public DeactivateUserCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        // 1. Self-Action Prevention
        if (user.Id == _currentUserService.UserId)
            return Result<bool>.Failure("CannotPerformActionOnSelf", "You cannot deactivate your own account.");

        // 2. Idempotency Check
        if (!user.IsActive)
            return Result<bool>.Success(true);

        user.IsActive = false;
        _unitOfWork.Users.Update(user);

        // 3. Revoke all sessions
        await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Audit Log
        _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} deactivated User {TargetUserId} ({PublicId})", 
            _currentUserService.UserId, user.Id, user.PublicId);

        return Result<bool>.Success(true);
    }
}
