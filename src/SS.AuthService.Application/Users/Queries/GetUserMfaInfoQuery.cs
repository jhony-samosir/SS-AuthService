using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public record GetUserMfaInfoQuery(Guid PublicId) : IRequest<Result<UserMfaInfoDto>>;
