using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Users.Handlers;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailQueue _emailQueue;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ResendVerificationEmailCommandHandler> _logger;

    public ResendVerificationEmailCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenHasher tokenHasher,
        IEmailQueue emailQueue,
        ICurrentUserService currentUserService,
        ILogger<ResendVerificationEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _emailQueue = emailQueue;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("UserNotFound", "User not found.");

        if (user.EmailVerifiedAt.HasValue)
            return Result<bool>.Failure("UserAlreadyVerified", "User email is already verified.");

        if (!user.IsActive)
            return Result<bool>.Failure("UserInactive", "Cannot resend verification for an inactive user.");

        // Business-Level Throttling: Prevent spamming (1-minute cooldown)
        var latestToken = await _unitOfWork.EmailVerifications.GetLatestByUserIdAsync(user.Id, cancellationToken);
        if (latestToken != null && latestToken.CreatedAt.AddMinutes(1) > DateTime.UtcNow)
        {
            return Result<bool>.Failure("ThrottlingActive", "Please wait 1 minute before resending the verification email.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Invalidate previous tokens
            await _unitOfWork.EmailVerifications.RemoveAllByUserIdAsync(user.Id, cancellationToken);

            // 2. Generate new token
            var verificationToken = _tokenHasher.Generate();
            var verificationTokenHash = _tokenHasher.Hash(verificationToken);

            var emailVerification = new EmailVerification
            {
                UserId = user.Id,
                VerificationTokenHash = verificationTokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailVerifications.AddAsync(emailVerification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 3. Queue email
            await _emailQueue.QueueEmailAsync(new EmailTask(user.Email, verificationToken, null, EmailType.Verification));

            _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} resent verification email to User {TargetUserId} ({Email})", 
                _currentUserService.UserId, user.Id, user.Email);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error resending verification email for user {UserId}", user.Id);
            throw;
        }
    }
}
