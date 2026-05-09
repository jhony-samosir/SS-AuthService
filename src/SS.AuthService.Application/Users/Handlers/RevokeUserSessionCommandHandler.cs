using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class RevokeUserSessionCommandHandler : IRequestHandler<RevokeUserSessionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RevokeUserSessionCommandHandler> _logger;

    public RevokeUserSessionCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ILogger<RevokeUserSessionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        var session = await _unitOfWork.AuthSessions.GetByPublicIdAsync(request.SessionPublicId, cancellationToken);
        if (session == null || session.UserId != user.Id)
            return Result<bool>.Failure("SessionNotFound", "Session not found for this user.");

        if (session.IsRevoked)
            return Result<bool>.Success(true);

        _unitOfWork.AuthSessions.Revoke(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} revoked Session {SessionId} for User {TargetUserId}", 
            _currentUserService.UserId, session.Id, user.Id);

        return Result<bool>.Success(true);
    }
}
