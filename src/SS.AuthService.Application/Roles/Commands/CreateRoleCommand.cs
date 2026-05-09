using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Roles.Commands;

public record CreateRoleCommand(
    string Name,
    string? Description
) : IRequest<Result<Guid>>;
