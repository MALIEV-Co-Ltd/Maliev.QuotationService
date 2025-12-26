using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Background service that registers Quotation Service permissions and roles with IAM.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class QuotationIAMRegistrationService : IAMRegistrationService
{
    public QuotationIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<QuotationIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "quotation")
    {
    }

    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return QuotationPermissions.GetPermissions().Select(p => new PermissionRegistration
        {
            PermissionId = p.Id,
            Description = p.Description
        });
    }

    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return QuotationPredefinedRoles.GetRoles().Select(r => new RoleRegistration
        {
            RoleId = $"roles.quotation.{r.Name}",
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}