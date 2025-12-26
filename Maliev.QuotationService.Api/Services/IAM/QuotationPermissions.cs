namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Defines permission constants and metadata for the Quotation Service.
/// Note: Constants include "Permission:" prefix for integration with ServiceDefaults policy provider.
/// </summary>
public static class QuotationPermissions
{
    // Quotation Operations
    public const string QuotationsCreate = "Permission:quotation.quotations.create";
    public const string QuotationsRead = "Permission:quotation.quotations.read";
    public const string QuotationsUpdate = "Permission:quotation.quotations.update";
    public const string QuotationsDelete = "Permission:quotation.quotations.delete";
    public const string QuotationsApprove = "Permission:quotation.quotations.approve";
    public const string QuotationsSend = "Permission:quotation.quotations.send";
    public const string QuotationsConvert = "Permission:quotation.quotations.convert";
    public const string QuotationsRevise = "Permission:quotation.quotations.revise";
    public const string QuotationsExpire = "Permission:quotation.quotations.expire";

    // Line Item Operations
    public const string LineItemsCreate = "Permission:quotation.line-items.create";
    public const string LineItemsRead = "Permission:quotation.line-items.read";
    public const string LineItemsUpdate = "Permission:quotation.line-items.update";
    public const string LineItemsDelete = "Permission:quotation.line-items.delete";

    // Template Operations
    public const string TemplatesCreate = "Permission:quotation.templates.create";
    public const string TemplatesRead = "Permission:quotation.templates.read";
    public const string TemplatesUse = "Permission:quotation.templates.use";

    public record PermissionMetadata(string Id, string Description);

    /// <summary>
    /// Gets all registered permission IDs for the Quotation Service.
    /// Strips the policy prefix for registration with IAM.
    /// </summary>
    public static IEnumerable<string> GetAll()
    {
        return GetPermissions().Select(p => p.Id);
    }

    /// <summary>
    /// Gets all registered permissions with descriptions for the Quotation Service.
    /// The Id returned here is the raw permission string (without prefix) for IAM.
    /// </summary>
    public static IEnumerable<PermissionMetadata> GetPermissions()
    {
        return new[]
        {
            new PermissionMetadata(QuotationsCreate.Replace("Permission:", ""), "Create new quotations"),
            new PermissionMetadata(QuotationsRead.Replace("Permission:", ""), "Read quotation details"),
            new PermissionMetadata(QuotationsUpdate.Replace("Permission:", ""), "Update quotation information"),
            new PermissionMetadata(QuotationsDelete.Replace("Permission:", ""), "Delete quotations"),
            new PermissionMetadata(QuotationsApprove.Replace("Permission:", ""), "Approve quotations"),
            new PermissionMetadata(QuotationsSend.Replace("Permission:", ""), "Send quotations to customers"),
            new PermissionMetadata(QuotationsConvert.Replace("Permission:", ""), "Convert quotation to order"),
            new PermissionMetadata(QuotationsRevise.Replace("Permission:", ""), "Create revised versions"),
            new PermissionMetadata(QuotationsExpire.Replace("Permission:", ""), "Mark quotations as expired"),
            new PermissionMetadata(LineItemsCreate.Replace("Permission:", ""), "Create quotation line items"),
            new PermissionMetadata(LineItemsRead.Replace("Permission:", ""), "Read line item details"),
            new PermissionMetadata(LineItemsUpdate.Replace("Permission:", ""), "Update line items"),
            new PermissionMetadata(LineItemsDelete.Replace("Permission:", ""), "Delete line items"),
            new PermissionMetadata(TemplatesCreate.Replace("Permission:", ""), "Create quotation templates"),
            new PermissionMetadata(TemplatesRead.Replace("Permission:", ""), "Read templates"),
            new PermissionMetadata(TemplatesUse.Replace("Permission:", ""), "Use templates for quotations")
        };
    }
}
