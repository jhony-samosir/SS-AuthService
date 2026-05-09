using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Security.DTOs;
using SS.AuthService.Application.Security.Queries;

namespace SS.AuthService.Application.Security.Handlers;

public class GetLoginAttemptByIdQueryHandler : IRequestHandler<GetLoginAttemptByIdQuery, LoginAttemptDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLoginAttemptByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginAttemptDto?> Handle(GetLoginAttemptByIdQuery request, CancellationToken cancellationToken)
    {
        var attempt = await _unitOfWork.LoginAttempts.GetByIdAsync(request.Id, cancellationToken);
        if (attempt == null) return null;

        Guid? userPublicId = null;
        if (attempt.UserId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(attempt.UserId.Value, cancellationToken);
            userPublicId = user?.PublicId;
        }

        return new LoginAttemptDto(
            attempt.Id,
            userPublicId,
            attempt.EmailAttempted,
            attempt.IpAddress.ToString(),
            attempt.DeviceInfo,
            attempt.IsSuccess,
            attempt.AttemptedAt
        );
    }
}
