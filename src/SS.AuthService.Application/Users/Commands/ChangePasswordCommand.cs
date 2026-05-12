using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result>;
