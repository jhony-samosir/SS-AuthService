using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record AssignUserRoleCommand(Guid PublicId, Guid RolePublicId) : IRequest<Result<bool>>;
