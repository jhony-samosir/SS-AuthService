using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
