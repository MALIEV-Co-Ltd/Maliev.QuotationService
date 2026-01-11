using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Background service that registers Quotation Service permissions and roles with IAM.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class QuotationIAMRegistrationService : IAMRegistrationService
{
    public QuotationIAMRegistrationService(
        IConfiguration configuration,
        ILogger<QuotationIAMRegistrationService> logger)
        : base(configuration, logger, "quotation")
    {
    }

    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return QuotationPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return QuotationPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
