namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Defines permission constants for the Quotation Service.
/// Follows GCP-style naming: {service}.{resource}.{action}
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

    /// <summary>
    /// Collection of all defined quotation permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { QuotationsCreate, "Create new quotations" },
        { QuotationsRead, "Read quotation details" },
        { QuotationsUpdate, "Update quotation information" },
        { QuotationsDelete, "Delete quotations" },
        { QuotationsApprove, "Approve quotations" },
        { QuotationsSend, "Send quotations to customers" },
        { QuotationsConvert, "Convert quotation to order" },
        { QuotationsRevise, "Create revised versions" },
        { QuotationsExpire, "Mark quotations as expired" },
        { LineItemsCreate, "Create quotation line items" },
        { LineItemsRead, "Read line item details" },
        { LineItemsUpdate, "Update line items" },
        { LineItemsDelete, "Delete line items" },
        { TemplatesCreate, "Create quotation templates" },
        { TemplatesRead, "Read templates" },
        { TemplatesUse, "Use templates for quotations" }
    };

    /// <summary>All available permission codes</summary>
    public static IEnumerable<string> All => AllWithDescriptions.Keys;

    /// <summary>
    /// Returns all available permission codes.
    /// </summary>
    public static IEnumerable<string> GetAll() => All;
}
