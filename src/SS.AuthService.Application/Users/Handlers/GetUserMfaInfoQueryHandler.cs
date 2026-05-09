using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;
using SS.AuthService.Application.Users.Queries;

namespace SS.AuthService.Application.Users.Handlers;

public class GetUserMfaInfoQueryHandler : IRequestHandler<GetUserMfaInfoQuery, Result<UserMfaInfoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserMfaInfoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserMfaInfoDto>> Handle(GetUserMfaInfoQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (user == null) return Result<UserMfaInfoDto>.Failure("UserNotFound", "User not found.");

        var count = await _unitOfWork.MfaRecoveryCodes.CountByUserIdAsync(user.Id, cancellationToken);

        return Result<UserMfaInfoDto>.Success(new UserMfaInfoDto(user.MfaEnabled, count));
    }
}
