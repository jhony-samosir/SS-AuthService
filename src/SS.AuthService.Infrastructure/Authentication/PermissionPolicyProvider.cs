using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SS.AuthService.Infrastructure.Authentication;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Format policy: "Permission:Menu:Action"
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName.Split(':');
            if (parts.Length == 3)
            {
                var menu = parts[1];
                var action = parts[2];

                return new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(menu, action))
                    .Build();
            }
        }

        return await base.GetPolicyAsync(policyName);
    }
}
