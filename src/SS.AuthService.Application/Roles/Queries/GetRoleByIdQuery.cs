using MediatR;
using SS.AuthService.Application.Roles.DTOs;

namespace SS.AuthService.Application.Roles.Queries;

public record GetRoleByIdQuery(Guid PublicId) : IRequest<RoleDto?>;
