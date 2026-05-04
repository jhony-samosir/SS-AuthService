using MediatR;
using SS.AuthService.Application.Users.DTOs;
using System;

namespace SS.AuthService.Application.Users.Queries;

public record GetUserDetailQuery(Guid PublicId) : IRequest<UserProfileDto?>;
