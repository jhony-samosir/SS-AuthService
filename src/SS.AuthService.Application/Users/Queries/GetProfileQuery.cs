using MediatR;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public record GetProfileQuery(
    Guid PublicId, 
    int LoggedInUserId, 
    bool IsAdmin) : IRequest<UserProfileDto?>;
