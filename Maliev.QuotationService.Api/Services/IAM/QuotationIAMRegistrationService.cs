using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Background service that registers Quotation Service permissions and roles with IAM.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class QuotationIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the QuotationIAMRegistrationService.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger.</param>
    public QuotationIAMRegistrationService(
        IConfiguration configuration,
        ILogger<QuotationIAMRegistrationService> logger)
        : base(configuration, logger, "quotation")
    {
    }

    /// <summary>
    /// Gets the permission registrations for the Quotation Service.
    /// </summary>
    /// <returns>Enumerable of permission registrations.</returns>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return QuotationPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <summary>
    /// Gets the predefined role registrations for the Quotation Service.
    /// </summary>
    /// <returns>Enumerable of role registrations.</returns>
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
