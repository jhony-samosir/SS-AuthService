using MediatR;
using SS.AuthService.Application.Common.Models;
using System;

namespace SS.AuthService.Application.Users.Commands;

public record UpdateUserCommand(
    Guid PublicId,
    string FullName,
    int RoleId,
    bool IsActive
) : IRequest<Result>;
