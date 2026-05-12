using MediatR;
using Microsoft.Extensions.Options;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Common.Settings;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Constants;
using SS.AuthService.Domain.Entities;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue _emailQueue;
    private readonly ITokenHasher _tokenHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly SecuritySettings _securitySettings;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork, 
        IEmailQueue emailQueue,
        ITokenHasher tokenHasher,
        ICurrentUserService currentUserService,
        IOptions<SecuritySettings> securitySettings)
    {
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
        _tokenHasher = tokenHasher;
        _currentUserService = currentUserService;
        _securitySettings = securitySettings.Value;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if email already exists
        var exists = await _unitOfWork.Users.ExistsByEmailAsync(request.Email.ToLower(), cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure("EmailAlreadyExists", "Email is already registered.");
        }

        // 2. Role Hierarchy & Validation
        var creatorId = _currentUserService.UserId;
        if (creatorId == null) return Result<Guid>.Failure("Unauthorized", "Unauthorized access.");

        var creator = await _unitOfWork.Users.GetByIdAsync(creatorId.Value, cancellationToken);
        if (creator == null) return Result<Guid>.Failure("Unauthorized", "Creator not found.");

        // Rule: Cannot assign a role with higher privilege (lower ID) than your own, unless SuperAdmin
        if (creator.RoleId != RoleConstants.SuperAdminRoleId && request.RoleId < creator.RoleId)
        {
            return Result<Guid>.Failure("InsufficientPrivilege", "You cannot assign a role with higher privileges than your own.");
        }

        var roleExists = await _unitOfWork.Users.RoleExistsAsync(request.RoleId, cancellationToken);
        if (!roleExists)
        {
            return Result<Guid>.Failure("InvalidRole", "The specified role does not exist.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 3. Create User entity
            var user = new User
            {
                PublicId = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                FullName = request.FullName,
                RoleId = request.RoleId,
                IsActive = request.IsActive,
                EmailVerifiedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // 4. Create Password Reset token
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 5. Queue Email (After Commit to ensure DB consistency)
            await _emailQueue.QueueEmailAsync(new EmailTask(user.Email, token, null, EmailType.Verification));

            return Result<Guid>.Success(user.PublicId);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
