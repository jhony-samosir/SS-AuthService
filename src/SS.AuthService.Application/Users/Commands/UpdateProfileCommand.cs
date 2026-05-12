using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Commands;

public record UpdateProfileCommand(
    string FullName
) : IRequest<Result>;
