using MediatR;
using SS.AuthService.Application.Security.DTOs;

namespace SS.AuthService.Application.Security.Queries;

public record GetLoginAttemptByIdQuery(long Id) : IRequest<LoginAttemptDto?>;
