using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Custom policy provider that dynamically creates policies for 'quotation.' permissions.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle both "quotation.xxx" and "Permission:quotation.xxx" formats
        var permission = policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase)
            ? policyName.Substring("Permission:".Length)
            : policyName;

        if (permission.StartsWith("quotation.", StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(permission));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
