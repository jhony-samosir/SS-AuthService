using Microsoft.AspNetCore.Authorization;

namespace SS.AuthService.Infrastructure.Authentication;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Menu { get; }
    public string Action { get; }

    public PermissionRequirement(string menu, string action)
    {
        Menu = menu;
        Action = action;
    }
}
