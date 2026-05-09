using MediatR;
using SS.AuthService.Application.Common.Models;
using System.Collections.Generic;

namespace SS.AuthService.Application.Users.Commands;

public record RegenerateRecoveryCodesCommand(Guid PublicId) : IRequest<Result<bool>>;
