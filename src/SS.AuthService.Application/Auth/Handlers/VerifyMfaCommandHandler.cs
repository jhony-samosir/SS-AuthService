using MediatR;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Auth.Handlers;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, LoginResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaService _mfaService;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenHasher _tokenHasher;

    public VerifyMfaCommandHandler(
        IUnitOfWork unitOfWork,
        IMfaService mfaService,
        IJwtProvider jwtProvider,
        ITokenHasher tokenHasher)
    {
        _unitOfWork = unitOfWork;
        _mfaService = mfaService;
        _jwtProvider = jwtProvider;
        _tokenHasher = tokenHasher;
    }

    public async Task<LoginResult> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate MFA Challenge Token
        var userId = _jwtProvider.ValidateMfaChallengeToken(request.MfaToken);
        if (userId == null)
        {
            return new LoginResult(false, "Invalid or expired MFA challenge.", StatusCode: 401);
        }

        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(userId.Value, cancellationToken);
        if (user == null || !user.IsActive || !user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
        {
            return new LoginResult(false, "Invalid user or MFA not enabled.", StatusCode: 401);
        }

        // 2. Security Check: Lockout status
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            return new LoginResult(false, "Account is locked. Please try again later.", StatusCode: 429);
        }

        // 3. Verify TOTP Code
        bool isValid = _mfaService.VerifyCode(user.MfaSecret, request.Code);
        
        // 4. Audit Trail & Brute Force Prevention (Transactional)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var attempt = new LoginAttempt
            {
                UserId = user.Id,
                EmailAttempted = user.Email,
                IpAddress = IPAddress.TryParse(request.IpAddress, out var ip) ? ip : IPAddress.None,
                DeviceInfo = request.DeviceInfo,
                IsSuccess = isValid,
                AttemptedAt = DateTime.UtcNow
            };

            await _unitOfWork.LoginAttempts.AddAsync(attempt, cancellationToken);

            if (isValid)
            {
                // Success: Reset failed attempts
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
            }
            else
            {
                // Failure: Increment failed attempts and lockout if needed
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                }
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!isValid)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new LoginResult(false, "Invalid MFA code.", StatusCode: 401);
            }

            // 5. Generate Session Tokens
            var accessToken = _jwtProvider.GenerateAccessToken(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();

            var session = new AuthSession
            {
                PublicId = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = _tokenHasher.Hash(refreshToken),
                IpAddress = attempt.IpAddress,
                DeviceInfo = request.DeviceInfo,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuthSessions.AddAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new LoginResult(true, "MFA verification successful.", accessToken, refreshToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
