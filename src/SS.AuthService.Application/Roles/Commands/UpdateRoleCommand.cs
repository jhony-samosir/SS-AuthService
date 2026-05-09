using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Roles.Commands;

public record UpdateRoleCommand(
    Guid PublicId,
    string Name,
    string? Description
) : IRequest<Result<bool>>;
