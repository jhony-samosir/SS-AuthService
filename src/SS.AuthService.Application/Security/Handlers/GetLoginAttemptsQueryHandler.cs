using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Security.DTOs;
using SS.AuthService.Application.Security.Queries;

namespace SS.AuthService.Application.Security.Handlers;

public class GetLoginAttemptsQueryHandler : IRequestHandler<GetLoginAttemptsQuery, PagedResult<LoginAttemptDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLoginAttemptsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<LoginAttemptDto>> Handle(GetLoginAttemptsQuery request, CancellationToken cancellationToken)
    {
        int? userId = null;
        if (request.UserPublicId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByPublicIdAsync(request.UserPublicId.Value, cancellationToken);
            if (user == null)
            {
                return new PagedResult<LoginAttemptDto>(new List<LoginAttemptDto>(), 0, request.PageNumber, request.PageSize);
            }
            userId = user.Id;
        }

        var (items, totalCount) = await _unitOfWork.LoginAttempts.GetPagedAsync(
            request.Email,
            userId,
            request.IpAddress,
            request.IsSuccess,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map UserId back to UserPublicId for the DTOs
        // 1. Get unique UserIds from items
        var userIds = items.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value).Distinct().ToList();
        
        // 2. Fetch PublicIds for those UserIds (Bulk Fetch - Fixes N+1 Problem)
        var userMap = new Dictionary<int, Guid>();
        if (userIds.Any())
        {
            var users = await _unitOfWork.Users.GetByIdsAsync(userIds, cancellationToken);
            userMap = users.ToDictionary(u => u.Id, u => u.PublicId);
        }

        var dtos = items.Select(i => new LoginAttemptDto(
            i.Id,
            i.UserId.HasValue && userMap.ContainsKey(i.UserId.Value) ? userMap[i.UserId.Value] : null,
            i.EmailAttempted,
            i.IpAddress.ToString(),
            i.DeviceInfo,
            i.IsSuccess,
            i.AttemptedAt
        )).ToList();

        return new PagedResult<LoginAttemptDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
