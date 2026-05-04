using MediatR;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User not found.");
        }

        // 1. Prevent self-deletion
        if (user.Id == _currentUserService.UserId)
        {
            return Result.Failure("SelfDeletionProhibited", "You cannot delete your own account.");
        }

        // 2. Prevent SuperAdmin deletion
        if (user.RoleId == RoleConstants.SuperAdminRoleId)
        {
            return Result.Failure("SuperAdminDeletionProhibited", "SuperAdmin accounts cannot be deleted.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 3. Soft Delete
            user.DeletedAt = System.DateTime.UtcNow;
            user.DeletedBy = _currentUserService.UserId;
            _unitOfWork.Users.Update(user);

            // 4. Revoke all active sessions (ExecuteUpdateAsync - immediate execution)
            await _unitOfWork.AuthSessions.RevokeAllForUserAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
