using MediatR;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using SS.AuthService.Domain.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User not found.");
        }

        // 1. Role Hierarchy & Validation
        var actorId = _currentUserService.UserId;
        if (actorId == null) return Result.Failure("Unauthorized", "Unauthorized access.");

        var actor = await _unitOfWork.Users.GetByIdAsync(actorId.Value, cancellationToken);
        if (actor == null) return Result.Failure("Unauthorized", "Actor not found.");

        // Rule: Cannot assign a role with higher privilege (lower ID) than your own, unless SuperAdmin
        if (actor.RoleId != RoleConstants.SuperAdminRoleId && request.RoleId < actor.RoleId)
        {
            return Result.Failure("InsufficientPrivilege", "You cannot assign a role with higher privileges than your own.");
        }

        // Rule: Cannot modify a SuperAdmin account unless you are SuperAdmin
        if (user.RoleId == RoleConstants.SuperAdminRoleId && actor.RoleId != RoleConstants.SuperAdminRoleId)
        {
            return Result.Failure("InsufficientPrivilege", "You do not have permission to modify a SuperAdmin account.");
        }

        var roleExists = await _unitOfWork.Users.RoleExistsAsync(request.RoleId, cancellationToken);
        if (!roleExists)
        {
            return Result.Failure("InvalidRole", "The specified role does not exist.");
        }

        user.FullName = request.FullName;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
