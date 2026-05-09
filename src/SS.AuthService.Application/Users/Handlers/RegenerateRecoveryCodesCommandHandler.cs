using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Entities;
using System.Security.Cryptography;

namespace SS.AuthService.Application.Users.Handlers;

public class RegenerateRecoveryCodesCommandHandler : IRequestHandler<RegenerateRecoveryCodesCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<RegenerateRecoveryCodesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RegenerateRecoveryCodesCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailQueue emailQueue,
        ILogger<RegenerateRecoveryCodesCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailQueue = emailQueue;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(RegenerateRecoveryCodesCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null) return Result<bool>.Failure("UserNotFound", "User not found.");

        if (!user.MfaEnabled) return Result<bool>.Failure("MfaNotEnabled", "MFA is not enabled for this user.");

        var rawRecoveryCodes = GenerateRecoveryCodes(10);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Remove old codes
            await _unitOfWork.MfaRecoveryCodes.RemoveAllByUserIdAsync(user.Id, cancellationToken);

            // 2. Insert new codes
            var recoveryCodeEntities = rawRecoveryCodes.Select(code => new MfaRecoveryCode
            {
                UserId = user.Id,
                CodeHash = _passwordHasher.HashPassword(code),
                IsUsed = false
            }).ToList();

            await _unitOfWork.MfaRecoveryCodes.AddRangeAsync(recoveryCodeEntities, cancellationToken);

            // 3. Invalidate all active sessions (Force re-login as security measure)
            await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 4. Queue Email (Send codes directly to user, Admin doesn't see them)
            await _emailQueue.QueueEmailAsync(new EmailTask(user.Email, null, rawRecoveryCodes, EmailType.MfaRecoveryCodes));

            _logger.LogWarning("SECURITY AUDIT: Admin {AdminId} regenerated MFA recovery codes for User {UserId} ({Email})", 
                _currentUserService.UserId, user.Id, user.Email);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error regenerating MFA recovery codes for user {UserId}", user.Id);
            throw;
        }
    }

    private List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            codes.Add($"{GenerateRandomString(5)}-{GenerateRandomString(5)}".ToUpper());
        }
        return codes;
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }
}
