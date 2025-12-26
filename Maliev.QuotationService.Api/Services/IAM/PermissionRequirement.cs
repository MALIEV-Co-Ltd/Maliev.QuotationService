using Microsoft.AspNetCore.Authorization;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Represents a requirement that a user must have a specific permission.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
