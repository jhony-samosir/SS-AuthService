using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;

namespace SS.AuthService.Application.Users.Handlers;

public class DisableUserMfaCommandHandler : IRequestHandler<DisableUserMfaCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DisableUserMfaCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DisableUserMfaCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DisableUserMfaCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DisableUserMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null) return Result<bool>.Failure("UserNotFound", "User not found.");

        if (!user.MfaEnabled) return Result<bool>.Failure("MfaAlreadyDisabled", "MFA is already disabled for this user.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            user.MfaEnabled = false;
            user.MfaSecret = null;
            _unitOfWork.Users.Update(user);

            // Invalidate all recovery codes
            await _unitOfWork.MfaRecoveryCodes.RemoveAllByUserIdAsync(user.Id, cancellationToken);

            // Invalidate all active sessions (Force re-login)
            await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} disabled MFA for User {UserId} ({Email})", 
                _currentUserService.UserId, user.Id, user.Email);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error disabling MFA for user {UserId}", user.Id);
            throw;
        }
    }
}
