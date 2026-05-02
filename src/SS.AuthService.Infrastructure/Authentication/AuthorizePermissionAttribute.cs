using Microsoft.AspNetCore.Authorization;

namespace SS.AuthService.Infrastructure.Authentication;

/// <summary>
/// Atribut kustom untuk otorisasi berbasis menu dan aksi (Create/Read/Update/Delete).
/// </summary>
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public string Menu { get; }
    public string Action { get; }

    public AuthorizePermissionAttribute(string menu, string action)
    {
        Menu = menu;
        Action = action;
        Policy = $"Permission:{menu}:{action}";
    }
}
