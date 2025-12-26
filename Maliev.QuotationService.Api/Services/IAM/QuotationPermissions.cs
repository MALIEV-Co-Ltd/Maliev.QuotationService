namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Defines permission constants and metadata for the Quotation Service.
/// </summary>
public static class QuotationPermissions
{
    // Quotation Operations
    public const string QuotationsCreate = "quotation.quotations.create";
    public const string QuotationsRead = "quotation.quotations.read";
    public const string QuotationsUpdate = "quotation.quotations.update";
    public const string QuotationsDelete = "quotation.quotations.delete";
    public const string QuotationsApprove = "quotation.quotations.approve";
    public const string QuotationsSend = "quotation.quotations.send";
    public const string QuotationsConvert = "quotation.quotations.convert";
    public const string QuotationsRevise = "quotation.quotations.revise";
    public const string QuotationsExpire = "quotation.quotations.expire";

    // Line Item Operations
    public const string LineItemsCreate = "quotation.line-items.create";
    public const string LineItemsRead = "quotation.line-items.read";
    public const string LineItemsUpdate = "quotation.line-items.update";
    public const string LineItemsDelete = "quotation.line-items.delete";

    // Template Operations
    public const string TemplatesCreate = "quotation.templates.create";
    public const string TemplatesRead = "quotation.templates.read";
    public const string TemplatesUse = "quotation.templates.use";

    public record PermissionMetadata(string Id, string Description);

    /// <summary>
    /// Gets all registered permission IDs for the Quotation Service.
    /// </summary>
    public static IEnumerable<string> GetAll()
    {
        return GetPermissions().Select(p => p.Id);
    }

    /// <summary>
    /// Gets all registered permissions with descriptions for the Quotation Service.
    /// </summary>
    public static IEnumerable<PermissionMetadata> GetPermissions()
    {
        return new[]
        {
            new PermissionMetadata(QuotationsCreate, "Create new quotations"),
            new PermissionMetadata(QuotationsRead, "Read quotation details"),
            new PermissionMetadata(QuotationsUpdate, "Update quotation information"),
            new PermissionMetadata(QuotationsDelete, "Delete quotations"),
            new PermissionMetadata(QuotationsApprove, "Approve quotations"),
            new PermissionMetadata(QuotationsSend, "Send quotations to customers"),
            new PermissionMetadata(QuotationsConvert, "Convert quotation to order"),
            new PermissionMetadata(QuotationsRevise, "Create revised versions"),
            new PermissionMetadata(QuotationsExpire, "Mark quotations as expired"),
            new PermissionMetadata(LineItemsCreate, "Create quotation line items"),
            new PermissionMetadata(LineItemsRead, "Read line item details"),
            new PermissionMetadata(LineItemsUpdate, "Update line items"),
            new PermissionMetadata(LineItemsDelete, "Delete line items"),
            new PermissionMetadata(TemplatesCreate, "Create quotation templates"),
            new PermissionMetadata(TemplatesRead, "Read templates"),
            new PermissionMetadata(TemplatesUse, "Use templates for quotations")
        };
    }
}