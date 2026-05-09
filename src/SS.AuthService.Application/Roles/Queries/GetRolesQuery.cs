using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Roles.DTOs;

namespace SS.AuthService.Application.Roles.Queries;

public record GetRolesQuery(
    string? SearchTerm = null,
    string SortBy = "CreatedAt",
    string SortDirection = "desc",
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<RoleListItemDto>>;
