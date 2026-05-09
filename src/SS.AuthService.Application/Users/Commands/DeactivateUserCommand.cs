using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record DeactivateUserCommand(Guid PublicId) : IRequest<Result<bool>>;
