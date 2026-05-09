using MediatR;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Auth.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterResult>
{
    private const string EnumerationSafeMessage =
        "If this email is not yet registered, a verification link has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailQueue _emailQueue;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationRepository emailVerificationRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IEmailQueue emailQueue)
    {
        _userRepository = userRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _emailQueue = emailQueue;
    }

    public async Task<RegisterResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Normalisasi email (case-insensitive storage)
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Account Enumeration Prevention: selalu kembalikan pesan yang sama
        var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
            return new RegisterResult(true, EnumerationSafeMessage);

        // 3. Hash password menggunakan Argon2id (Enterprise Best Practice)
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Ambil default role dari DB (bukan magic number)
        var defaultRoleId = await _userRepository.GetDefaultCustomerRoleIdAsync(cancellationToken);

        // 5. Transaksi ACID
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                FullName = request.FullName.Trim(),
                IsActive = true,
                EmailVerifiedAt = null,
                TosAcceptedAt = DateTime.UtcNow,
                PrivacyPolicyAcceptedAt = DateTime.UtcNow,
                RoleId = defaultRoleId
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Generate token acak -> hash SHA-256 (deterministik, bisa di-lookup dari DB)
            //    BUKAN Argon2id karena Argon2id menghasilkan hash berbeda setiap kali (tidak bisa di-lookup)
            var verificationToken = _tokenHasher.Generate();
            var verificationTokenHash = _tokenHasher.Hash(verificationToken);

            var emailVerification = new EmailVerification
            {
                UserId = user.Id,
                VerificationTokenHash = verificationTokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            await _emailVerificationRepository.AddAsync(emailVerification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 7. Masukkan email ke antrean background (UX Best Practice)
            await _emailQueue.QueueEmailAsync(new EmailTask(normalizedEmail, verificationToken, null, EmailType.Verification));

            return new RegisterResult(true, EnumerationSafeMessage);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
