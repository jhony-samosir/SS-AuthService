using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Security.DTOs;

namespace SS.AuthService.Application.Security.Queries;

public record GetLoginAttemptsQuery(
    string? Email = null,
    Guid? UserPublicId = null,
    string? IpAddress = null,
    bool? IsSuccess = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PagedResult<LoginAttemptDto>>;
