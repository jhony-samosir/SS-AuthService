using MediatR;
using SS.AuthService.Application.Common.Models;
using System;

namespace SS.AuthService.Application.Users.Commands;

public record UnlockUserCommand(Guid PublicId) : IRequest<Result>;
