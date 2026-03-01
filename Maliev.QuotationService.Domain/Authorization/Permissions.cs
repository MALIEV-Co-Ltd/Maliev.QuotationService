namespace Maliev.QuotationService.Domain.Authorization;

/// <summary>
/// Permission constants for Quotation Service endpoints.
/// </summary>
public static class Permissions
{
    public const string QuotationsCreate = "quotation.quotations.create";
    public const string QuotationsRead = "quotation.quotations.read";
    public const string QuotationsUpdate = "quotation.quotations.update";
    public const string QuotationsDelete = "quotation.quotations.delete";
    public const string QuotationsApprove = "quotation.quotations.approve";

    public const string RfqsCreate = QuotationsCreate;
    public const string RfqsRead = QuotationsRead;
    public const string RfqsUpdate = QuotationsUpdate;

    public const string CustomersRead = "quotation.customers.read";
    public const string CustomersUpdate = "quotation.customers.update";

    public const string AnalyticsRead = QuotationsRead;
    public const string MetricsRead = QuotationsRead;

    /// <summary>
    /// All permission constants.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        QuotationsCreate,
        QuotationsRead,
        QuotationsUpdate,
        QuotationsDelete,
        QuotationsApprove,
        RfqsCreate,
        RfqsRead,
        RfqsUpdate,
        CustomersRead,
        CustomersUpdate,
        AnalyticsRead,
        MetricsRead
    ];
}
