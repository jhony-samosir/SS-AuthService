using System.Diagnostics;
using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Options;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailQueue _emailQueue;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SecuritySettings _securitySettings;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork, 
        ITokenHasher tokenHasher, 
        IEmailQueue emailQueue,
        IPasswordHasher passwordHasher,
        IOptions<SecuritySettings> securitySettings)
    {
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _emailQueue = emailQueue;
        _passwordHasher = passwordHasher;
        _securitySettings = securitySettings.Value;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Start Stopwatch for Constant-Time Response (Enterprise Best Practice)
        var sw = Stopwatch.StartNew();

        // 2. Cari user
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        
        // 3. Mitigasi Timing Attack: Lakukan alur kerja kriptografi yang sama
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string tokenHash = _tokenHasher.Hash(token);

        if (user != null)
        {
            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                ResetTokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_securitySettings.PasswordResetExpiryMinutes),
                IsUsed = false
            };

            await _unitOfWork.PasswordResets.AddAsync(passwordReset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailQueue.QueueEmailAsync(new EmailTask(user.Email, token, null, EmailType.Verification));
        }
        else
        {
            // Fake operation: Hashing dummy untuk menyamakan CPU cost
            _passwordHasher.HashPassword(token); 
        }

        // 4. Constant-Time Enforcement
        sw.Stop();
        var remainingMs = _securitySettings.ConstantTimeResponseMs - (int)sw.ElapsedMilliseconds;
        
        if (remainingMs > 0)
        {
            await Task.Delay(remainingMs, cancellationToken);
        }
    }
}
