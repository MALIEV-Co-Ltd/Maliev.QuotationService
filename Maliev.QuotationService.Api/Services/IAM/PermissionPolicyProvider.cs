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
        var normalizedPermission = NormalizePermissionPolicy(policyName);
        if (normalizedPermission.StartsWith("quotation.", StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(normalizedPermission));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    private static string NormalizePermissionPolicy(string policyName)
    {
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            var rawPermission = policyName["Permission:".Length..];
            return rawPermission.Split(':', 2, StringSplitOptions.TrimEntries)[0];
        }

        return policyName;
    }
}
