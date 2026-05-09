using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record RevokeUserSessionCommand(Guid UserPublicId, Guid SessionPublicId) : IRequest<Result<bool>>;
