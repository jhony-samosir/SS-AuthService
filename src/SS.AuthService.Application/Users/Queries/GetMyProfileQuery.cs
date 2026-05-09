using MediatR;
using SS.AuthService.Application.Users.DTOs;

namespace SS.AuthService.Application.Users.Queries;

public record GetMyProfileQuery() : IRequest<UserProfileDto?>;
