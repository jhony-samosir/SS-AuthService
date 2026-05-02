using MediatR;

namespace SS.AuthService.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;
