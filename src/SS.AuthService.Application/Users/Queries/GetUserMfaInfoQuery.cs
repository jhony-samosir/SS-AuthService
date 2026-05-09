using MediatR;
using SS.AuthService.Application.Common.Models;

namespace SS.AuthService.Application.Users.Queries;

public record UserMfaInfoDto(bool IsEnabled, int RecoveryCodesRemaining);

public record GetUserMfaInfoQuery(Guid PublicId) : IRequest<Result<UserMfaInfoDto>>;
