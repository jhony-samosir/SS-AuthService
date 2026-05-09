using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class RevokeAllUserSessionsCommandHandler : IRequestHandler<RevokeAllUserSessionsCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RevokeAllUserSessionsCommandHandler> _logger;

    public RevokeAllUserSessionsCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ILogger<RevokeAllUserSessionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RevokeAllUserSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} revoked ALL Sessions for User {TargetUserId}", 
            _currentUserService.UserId, user.Id);

        return Result<bool>.Success(true);
    }
}
