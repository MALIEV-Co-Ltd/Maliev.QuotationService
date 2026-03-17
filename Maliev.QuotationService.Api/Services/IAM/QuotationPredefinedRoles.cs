namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Predefined roles for the Quotation Service.
/// Roles follow the GCP format: roles.quotation.{role-name}
/// </summary>
public static class QuotationPredefinedRoles
{
    /// <summary>
    /// Full access to all quotation resources and management operations.
    /// </summary>
    public const string Admin = "roles.quotation.admin";
    /// <summary>
    /// Can manage quotations, approve them, and manage templates.
    /// </summary>
    public const string Manager = "roles.quotation.manager";
    /// <summary>
    /// Can create and send quotations, and use templates.
    /// </summary>
    public const string Creator = "roles.quotation.creator";
    /// <summary>
    /// Read-only access to quotations and templates.
    /// </summary>
    public const string Viewer = "roles.quotation.viewer";

    /// <summary>
    /// All predefined roles with their descriptions and permissions.
    /// </summary>
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
