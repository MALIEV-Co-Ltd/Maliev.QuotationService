using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Response model for a quotation.
/// </summary>
public class QuotationResponse
{
    /// <summary>
    /// The unique identifier of the quotation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The customer details.
    /// </summary>
    public CustomerDto? Customer { get; set; }

    /// <summary>
    /// The unique identifier of the source RFQ, if any.
    /// </summary>
    public Guid? SourceRfqId { get; set; }

    /// <summary>
    /// The current version number of the quotation.
    /// </summary>
    public int CurrentVersionNumber { get; set; }

    /// <summary>
    /// The current status of the quotation.
    /// </summary>
    public QuotationStatus Status { get; set; }

    /// <summary>
    /// The start date of the validity period.
    /// </summary>
    public DateTime ValidityPeriodStart { get; set; }

    /// <summary>
    /// The end date of the validity period.
    /// </summary>
    public DateTime ValidityPeriodEnd { get; set; }

    /// <summary>
    /// The timestamp when the quotation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the quotation was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
