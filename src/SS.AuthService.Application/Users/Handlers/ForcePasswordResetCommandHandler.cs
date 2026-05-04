using MediatR;
using Microsoft.Extensions.Options;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Entities;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class ForcePasswordResetCommandHandler : IRequestHandler<ForcePasswordResetCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue _emailQueue;
    private readonly ITokenHasher _tokenHasher;
    private readonly SecuritySettings _securitySettings;

    public ForcePasswordResetCommandHandler(
        IUnitOfWork unitOfWork, 
        IEmailQueue emailQueue,
        ITokenHasher tokenHasher,
        IOptions<SecuritySettings> securitySettings)
    {
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
        _tokenHasher = tokenHasher;
        _securitySettings = securitySettings.Value;
    }

    public async Task<Result> Handle(ForcePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User not found.");
        }

        // 1. Create Password Reset token
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var passwordReset = new PasswordReset
        {
            UserId = user.Id,
            ResetTokenHash = _tokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_securitySettings.PasswordResetExpiryMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PasswordResets.AddAsync(passwordReset, cancellationToken);
        
        // 2. Save Changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Queue Email
        await _emailQueue.QueueEmailAsync(new EmailTask(user.Email, token));

        return Result.Success();
    }
}
