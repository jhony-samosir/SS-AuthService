using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnlockUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User not found.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = System.DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
