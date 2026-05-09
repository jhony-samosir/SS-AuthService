using MediatR;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public record GetUserSessionsQuery(Guid UserPublicId) : IRequest<List<UserSessionDto>?>;
