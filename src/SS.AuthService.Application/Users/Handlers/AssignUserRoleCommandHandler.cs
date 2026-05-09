using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AssignUserRoleCommandHandler> _logger;

    public AssignUserRoleCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ILogger<AssignUserRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        // 1. Self-Action Prevention (Demotion prevention)
        if (user.Id == _currentUserService.UserId)
            return Result<bool>.Failure("CannotPerformActionOnSelf", "You cannot change your own role.");

        var role = await _unitOfWork.Roles.GetByPublicIdAsync(request.RolePublicId, cancellationToken);
        if (role == null)
            return Result<bool>.Failure("RoleNotFound", "Role not found.");

        // 2. Idempotency Check
        if (user.RoleId == role.Id)
            return Result<bool>.Success(true);

        int oldRoleId = user.RoleId;
        user.RoleId = role.Id;
        _unitOfWork.Users.Update(user);

        // 3. Revoke sessions to force new token with new claims
        await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Audit Log
        _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} changed Role of User {TargetUserId} ({PublicId}) from {OldRole} to {NewRole}", 
            _currentUserService.UserId, user.Id, user.PublicId, oldRoleId, role.Id);

        return Result<bool>.Success(true);
    }
}
