using MediatR;
using Microsoft.EntityFrameworkCore;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Auth.Handlers;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork, 
        ITokenHasher tokenHasher, 
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(request.Token);
        var reset = await _unitOfWork.PasswordResets.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (reset == null || reset.IsUsed || reset.ExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure("InvalidToken", "Invalid or expired reset token.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(reset.UserId, cancellationToken);
        if (user == null) 
        {
            return Result.Failure("InvalidToken", "Invalid or expired reset token.");
        }

        // 1. Password History Check (Last 3)
        var lastPasswords = await _unitOfWork.PasswordHistories.GetLastPasswordsAsync(user.Id, 3, cancellationToken);
        foreach (var history in lastPasswords)
        {
            if (_passwordHasher.VerifyPassword(request.NewPassword, history.PasswordHash))
            {
                return Result.Failure("PasswordUsedRecently", "You have used this password recently. Please choose a different one.");
            }
        }

        // 2. Begin Transaction (Atomic Update)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // A. Update User Password
            var newHash = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = newHash;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);

            // B. Mark Token as Used
            reset.IsUsed = true;
            reset.UsedAt = DateTime.UtcNow;
            _unitOfWork.PasswordResets.Update(reset);

            // C. Add to History
            var history = new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = newHash,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PasswordHistories.AddAsync(history, cancellationToken);

            // D. Global Sign Out (Revoke All Sessions)
            await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
