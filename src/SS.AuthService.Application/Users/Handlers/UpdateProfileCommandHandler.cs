using MediatR;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Users.Handlers;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure("Unauthorized", "You must be logged in to update your profile.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return Result.Failure("UserNotFound", "User record could not be found.");
        }

        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userId;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
