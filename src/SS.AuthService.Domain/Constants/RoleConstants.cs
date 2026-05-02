namespace SS.AuthService.Domain.Constants;

public static class RoleConstants
{
    public const int AdminRoleId = 1;
    public const int CustomerRoleId = 2;
    
    // Helper to get string version for IsInRole checks
    public static string AdminRoleName => AdminRoleId.ToString();
}
