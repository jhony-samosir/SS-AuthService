using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record RevokeAllUserSessionsCommand(Guid UserPublicId) : IRequest<Result<bool>>;
