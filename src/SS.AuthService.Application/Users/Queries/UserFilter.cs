using System;

namespace SS.AuthService.Application.Users.Queries;

public record UserFilter(
    string? Email,
    string? FullName,
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
