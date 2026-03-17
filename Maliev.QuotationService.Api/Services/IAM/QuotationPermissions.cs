namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Defines permission constants for the Quotation Service.
/// Follows GCP-style naming: {service}.{resource}.{action}
/// </summary>
public static class QuotationPermissions
{
    /// <summary>
    /// Permission to create new quotations.
    /// </summary>
    public const string QuotationsCreate = "quotation.quotations.create";
    /// <summary>
    /// Permission to read quotation details.
    /// </summary>
    public const string QuotationsRead = "quotation.quotations.read";
    /// <summary>
    /// Permission to update quotation information.
    /// </summary>
    public const string QuotationsUpdate = "quotation.quotations.update";
    /// <summary>
    /// Permission to delete quotations.
    /// </summary>
    public const string QuotationsDelete = "quotation.quotations.delete";
    /// <summary>
    /// Permission to approve quotations.
    /// </summary>
    public const string QuotationsApprove = "quotation.quotations.approve";
    /// <summary>
    /// Permission to send quotations to customers.
    /// </summary>
    public const string QuotationsSend = "quotation.quotations.send";
    /// <summary>
    /// Permission to convert quotation to order.
    /// </summary>
    public const string QuotationsConvert = "quotation.quotations.convert";
    /// <summary>
    /// Permission to create revised versions of quotations.
    /// </summary>
    public const string QuotationsRevise = "quotation.quotations.revise";
    /// <summary>
    /// Permission to mark quotations as expired.
    /// </summary>
    public const string QuotationsExpire = "quotation.quotations.expire";

    // Line Item Operations
    /// <summary>
    /// Permission to create quotation line items.
    /// </summary>
    public const string LineItemsCreate = "quotation.line-items.create";
    /// <summary>
    /// Permission to read line item details.
    /// </summary>
    public const string LineItemsRead = "quotation.line-items.read";
    /// <summary>
    /// Permission to update line items.
    /// </summary>
    public const string LineItemsUpdate = "quotation.line-items.update";
    /// <summary>
    /// Permission to delete line items.
    /// </summary>
    public const string LineItemsDelete = "quotation.line-items.delete";

    // Template Operations
    /// <summary>
    /// Permission to create quotation templates.
    /// </summary>
    public const string TemplatesCreate = "quotation.templates.create";
    /// <summary>
    /// Permission to read templates.
    /// </summary>
    public const string TemplatesRead = "quotation.templates.read";
    /// <summary>
    /// Permission to use templates for quotations.
    /// </summary>
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
