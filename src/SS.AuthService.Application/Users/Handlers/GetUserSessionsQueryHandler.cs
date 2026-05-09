using MediatR;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Users.DTOs;
using SS.AuthService.Application.Users.Queries;

namespace SS.AuthService.Application.Users.Handlers;

public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, List<UserSessionDto>?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSessionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserSessionDto>?> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
        if (user == null)
            return null;

        var sessions = await _unitOfWork.AuthSessions.GetByUserIdAsync(user.Id, cancellationToken: cancellationToken);

        return sessions.Select(s => new UserSessionDto(
            s.PublicId,
            s.DeviceInfo,
            s.IpAddress?.ToString(),
            s.CreatedAt,
            s.ExpiresAt,
            s.IsRevoked
        )).ToList();
    }
}
