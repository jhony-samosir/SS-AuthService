using System;

namespace SS.AuthService.Application.Users.Queries;

public record UserFilter(
    string? SearchTerm,
    string? Email,
    string? FullName,
    string? RoleName,
    int? RoleId,
    bool? IsActive,
    bool? MfaEnabled,
    bool? IsLocked,
    DateTime? CreatedAtFrom,
    DateTime? CreatedAtTo,
    string SortBy,
    string SortDirection,
    int PageNumber,
    int PageSize
);
