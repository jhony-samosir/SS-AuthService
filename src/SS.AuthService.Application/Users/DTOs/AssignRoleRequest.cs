namespace SS.AuthService.Application.Users.DTOs;

/// <summary>Request body untuk endpoint Assign Role ke User.</summary>
public record AssignRoleRequest(Guid RolePublicId);
