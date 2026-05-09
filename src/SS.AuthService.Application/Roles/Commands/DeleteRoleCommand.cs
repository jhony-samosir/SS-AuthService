using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Roles.Commands;

public record DeleteRoleCommand(Guid PublicId) : IRequest<Result<bool>>;
