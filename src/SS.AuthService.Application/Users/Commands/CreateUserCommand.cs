using MediatR;
using SS.AuthService.Application.Common.Models;
using System;

namespace SS.AuthService.Application.Users.Commands;

public record CreateUserCommand(
    string Email,
    string FullName,
    int RoleId,
    bool IsActive = true
) : IRequest<Result<Guid>>;
