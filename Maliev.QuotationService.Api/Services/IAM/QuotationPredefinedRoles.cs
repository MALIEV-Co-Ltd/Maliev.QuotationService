namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Predefined roles for the Quotation Service.
/// Roles follow the GCP format: roles.quotation.{role-name}
/// </summary>
public static class QuotationPredefinedRoles
{
    public const string Admin = "roles.quotation.admin";
    public const string Manager = "roles.quotation.manager";
    public const string Creator = "roles.quotation.creator";
    public const string Viewer = "roles.quotation.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Admin, "Full access to all quotation resources and management operations.", QuotationPermissions.All.ToArray()),

        (Manager, "Can manage quotations, approve them, and manage templates.", new[]
        {
            QuotationPermissions.QuotationsCreate,
            QuotationPermissions.QuotationsRead,
            QuotationPermissions.QuotationsUpdate,
            QuotationPermissions.QuotationsApprove,
            QuotationPermissions.QuotationsSend,
            QuotationPermissions.QuotationsConvert,
            QuotationPermissions.QuotationsRevise,
            QuotationPermissions.LineItemsCreate,
            QuotationPermissions.LineItemsRead,
            QuotationPermissions.LineItemsUpdate,
            QuotationPermissions.LineItemsDelete,
            QuotationPermissions.TemplatesCreate,
            QuotationPermissions.TemplatesRead,
            QuotationPermissions.TemplatesUse
        }),

        (Creator, "Can create and send quotations, and use templates.", new[]
        {
            QuotationPermissions.QuotationsCreate,
            QuotationPermissions.QuotationsRead,
            QuotationPermissions.QuotationsUpdate,
            QuotationPermissions.QuotationsSend,
            QuotationPermissions.LineItemsCreate,
            QuotationPermissions.LineItemsRead,
            QuotationPermissions.LineItemsUpdate,
            QuotationPermissions.TemplatesRead,
            QuotationPermissions.TemplatesUse
        }),

        (Viewer, "Read-only access to quotations and templates.", new[]
        {
            QuotationPermissions.QuotationsRead,
            QuotationPermissions.LineItemsRead,
            QuotationPermissions.TemplatesRead
        })
    };
}
