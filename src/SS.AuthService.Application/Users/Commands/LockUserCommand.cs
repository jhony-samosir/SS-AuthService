using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record LockUserCommand(
    Guid PublicId, 
    DateTime? LockedUntil = null, 
    int? LockDurationMinutes = null) : IRequest<Result<bool>>;
