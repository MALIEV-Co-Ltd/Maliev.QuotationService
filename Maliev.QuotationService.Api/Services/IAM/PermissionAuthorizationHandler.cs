using Microsoft.AspNetCore.Authorization;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Authorization handler that checks if the user has the required permission in their JWT claims.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // The 'permissions' claim is populated by AuthService after resolving from IAMService
        if (context.User.HasClaim(c => c.Type == "permissions" && c.Value == requirement.Permission))
        {
            _logger.LogDebug("User authorized for permission: {Permission}", requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User is NOT authorized for permission: {Permission}", requirement.Permission);
        }

        return Task.CompletedTask;
    }
}
