using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class LockUserCommandHandler : IRequestHandler<LockUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LockUserCommandHandler> _logger;

    public LockUserCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ILogger<LockUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        // 1. Self-Action Prevention
        if (user.Id == _currentUserService.UserId)
            return Result<bool>.Failure("CannotPerformActionOnSelf", "You cannot lock your own account.");

        DateTime lockTime;
        if (request.LockedUntil.HasValue)
        {
            lockTime = request.LockedUntil.Value;
        }
        else if (request.LockDurationMinutes.HasValue)
        {
            lockTime = DateTime.UtcNow.AddMinutes(request.LockDurationMinutes.Value);
        }
        else
        {
            return Result<bool>.Failure("InvalidLockRequest", "Either LockedUntil or LockDurationMinutes must be provided.");
        }

        // 2. Idempotency Check (if same lock time, skip)
        if (user.LockedUntil == lockTime)
            return Result<bool>.Success(true);

        user.LockedUntil = lockTime;
        _unitOfWork.Users.Update(user);

        // 3. Revoke all sessions when locking
        await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Audit Log
        _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} locked User {TargetUserId} ({PublicId}) until {LockedUntil}", 
            _currentUserService.UserId, user.Id, user.PublicId, lockTime);

        return Result<bool>.Success(true);
    }
}
