using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record DisableUserMfaCommand(Guid PublicId) : IRequest<Result<bool>>;
