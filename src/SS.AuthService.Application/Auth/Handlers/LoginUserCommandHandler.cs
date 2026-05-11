using MediatR;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Application.Interfaces;
using Microsoft.Extensions.Options;
using SS.AuthService.Domain.Entities;
using System.Net;

namespace SS.AuthService.Application.Auth.Handlers;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenHasher _tokenHasher;
    private readonly SecuritySettings _securitySettings;

    // Valid Argon2 format hash to prevent timing attacks when email is not found
    private const string DummyHash = "MTIzNDU2Nzg5MDEyMzQ1Ng==.YTM0NWY2NzhhYmNkZWYwMTIzNDU2NzhhYmNkZWYwMTI=";

    public LoginUserCommandHandler(
        IUnitOfWork unitOfWork, 
        IPasswordHasher passwordHasher, 
        IJwtProvider jwtProvider,
        ITokenHasher tokenHasher,
        IOptionsSnapshot<SecuritySettings> securitySettings)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _tokenHasher = tokenHasher;
        _securitySettings = securitySettings.Value;
    }

    public async Task<LoginResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Cari User (Account Enumeration Prevention)
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(request.Email.ToLower(), cancellationToken);
        
        // Pesan error generik untuk semua kegagalan kredensial
        const string invalidCredentialsMessage = "Invalid credentials.";

        // 2. Cek Lockout
        if (user != null && user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            return new LoginResult(false, "Account is locked. Please try again later.", StatusCode: 429);
        }

        // 3. Verifikasi Kelengkapan Data User (Role required for token generation)
        if (user != null && user.Role == null)
        {
            return new LoginResult(false, "Account configuration error. Please contact administrator.", StatusCode: 500);
        }

        bool isPasswordValid = false;
        if (user != null)
        {
            // 3. Verifikasi Password
            isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash ?? "");
        }
        else
        {
            // Dummy check with VALID format to ensure the hasher actually runs and takes time
            _passwordHasher.VerifyPassword(request.Password, DummyHash);
        }

        // 4. Audit Trail & Brute Force Prevention (Wajib pakai Transaction)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var attempt = new LoginAttempt
            {
                UserId = user?.Id,
                EmailAttempted = request.Email,
                IpAddress = IPAddress.TryParse(request.IpAddress, out var ip) ? ip : IPAddress.None,
                DeviceInfo = request.DeviceInfo,
                IsSuccess = isPasswordValid,
                AttemptedAt = DateTime.UtcNow
            };

            await _unitOfWork.LoginAttempts.AddAsync(attempt, cancellationToken);

            if (user != null)
            {
                if (isPasswordValid)
                {
                    // Berhasil: Reset failed attempts
                    user.FailedLoginAttempts = 0;
                    user.LockedUntil = null;
                }
                else
                {
                    // Gagal: Tambah failed attempts
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                    }
                }
                _unitOfWork.Users.Update(user);
            }

            if (!isPasswordValid)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new LoginResult(false, invalidCredentialsMessage, StatusCode: 401);
            }

            // 5. Cek Verifikasi Email (Hanya jika password benar)
            if (user!.EmailVerifiedAt == null)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new LoginResult(false, "Please verify your email before logging in.", StatusCode: 403);
            }

            // 6. Session Management
            if (user.MfaEnabled)
            {
                var mfaToken = _jwtProvider.GenerateMfaChallengeToken(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new LoginResult(true, "MFA required.", AccessToken: mfaToken, StatusCode: 202, IsMfaRequired: true);
            }

            var accessToken = _jwtProvider.GenerateAccessToken(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();

            var session = new AuthSession
            {
                PublicId = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = _tokenHasher.Hash(refreshToken), // Di-hash menggunakan SHA-256
                IpAddress = attempt.IpAddress,
                DeviceInfo = request.DeviceInfo,
                ExpiresAt = DateTime.UtcNow.AddDays(_securitySettings.RefreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuthSessions.AddAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new LoginResult(true, "Login successful.", accessToken, refreshToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
