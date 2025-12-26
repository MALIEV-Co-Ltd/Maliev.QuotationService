namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Defines predefined roles and their associated permissions for the Quotation Service.
/// </summary>
public static class QuotationPredefinedRoles
{
    public const string Admin = "quotation-admin";
    public const string Manager = "quotation-manager";
    public const string Creator = "quotation-creator";
    public const string Viewer = "quotation-viewer";

    public record RoleMetadata(string Name, string Description, IEnumerable<string> Permissions);

    public static IEnumerable<RoleMetadata> GetRoles()
    {
        var allPermissions = QuotationPermissions.GetPermissions().Select(p => p.Id).ToList();

        yield return new RoleMetadata(
            Admin,
            "Full access to all quotation resources and management operations.",
            allPermissions);

        yield return new RoleMetadata(
            Manager,
            "Can manage quotations, approve them, and manage templates.",
            new[]
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
            });

        yield return new RoleMetadata(
            Creator,
            "Can create and send quotations, and use templates.",
            new[]
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
            });

        yield return new RoleMetadata(
            Viewer,
            "Read-only access to quotations and templates.",
            new[]
            {
                QuotationPermissions.QuotationsRead,
                QuotationPermissions.LineItemsRead,
                QuotationPermissions.TemplatesRead
            });
    }
}