using MediatR;
using Microsoft.Extensions.Options;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using System.Net;

namespace SS.AuthService.Application.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenHasher _tokenHasher;
    private readonly SecuritySettings _securitySettings;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtProvider jwtProvider,
        ITokenHasher tokenHasher,
        IOptionsSnapshot<SecuritySettings> securitySettings)
    {
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
        _tokenHasher = tokenHasher;
        _securitySettings = securitySettings.Value;
    }

    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Hash the incoming refresh token to look it up in the database
        var refreshTokenHash = _tokenHasher.Hash(request.RefreshToken);

        // 2. Find the session in the database
        var session = await _unitOfWork.AuthSessions.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);

        // 3. Reuse Attack Detection & Validation
        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return new LoginResult(false, "Invalid or expired session.", StatusCode: 401);
        }

        if (session.IsRevoked)
        {
            // Critical Security Incident: Token reuse detected. Revoke all active sessions for this user.
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.AuthSessions.RevokeAllForUserAsync(session.UserId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
            
            return new LoginResult(false, "Security alert: session compromised. All sessions revoked.", StatusCode: 401);
        }

        // 4. Get User to generate new tokens
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(session.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new LoginResult(false, "User account is disabled or not found.", StatusCode: 401);
        }

        if (user.Role == null)
        {
            return new LoginResult(false, "Account configuration error. Please contact administrator.", StatusCode: 500);
        }

        // 5. Refresh Token Rotation (Transactional)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Revoke old session
            _unitOfWork.AuthSessions.Revoke(session);

            // Generate new tokens
            var newAccessToken = _jwtProvider.GenerateAccessToken(user);
            var newRefreshToken = _jwtProvider.GenerateRefreshToken();

            // Create new session
            var newSession = new AuthSession
            {
                PublicId = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = _tokenHasher.Hash(newRefreshToken),
                IpAddress = IPAddress.TryParse(request.IpAddress, out var ip) ? ip : IPAddress.None,
                DeviceInfo = request.DeviceInfo,
                ExpiresAt = DateTime.UtcNow.AddDays(_securitySettings.RefreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuthSessions.AddAsync(newSession, cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new LoginResult(true, "Token refreshed successfully.", newAccessToken, newRefreshToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
