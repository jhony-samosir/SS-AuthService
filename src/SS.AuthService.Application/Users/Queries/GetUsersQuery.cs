using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Users.DTOs;
using System;

namespace SS.AuthService.Application.Users.Queries;

public record GetUsersQuery(
    string? SearchTerm = null,
    string? Email = null,
    string? FullName = null,
    string? RoleName = null,
    int? RoleId = null,
    bool? IsActive = null,
    bool? MfaEnabled = null,
    bool? IsLocked = null,
    DateTime? CreatedAtFrom = null,
    DateTime? CreatedAtTo = null,
    string SortBy = "CreatedAt",
    string SortDirection = "desc",
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<UserListItemDto>>;
