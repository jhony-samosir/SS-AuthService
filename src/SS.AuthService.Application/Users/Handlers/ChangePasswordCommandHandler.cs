using MediatR;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure("Unauthorized", "You must be logged in to change your password.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User record could not be found.");
        }

        // 1. Verify current password
        if (string.IsNullOrEmpty(user.PasswordHash) || 
            !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("InvalidCurrentPassword", "The current password provided is incorrect.");
        }

        // 2. Hash and update new password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userId;

        _unitOfWork.Users.Update(user);

        // 3. Security: Revoke all sessions so the user has to log in again on all devices
        await _unitOfWork.AuthSessions.RevokeAllForUserAsync(userId.Value, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
