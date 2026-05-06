using System.ComponentModel.DataAnnotations;
using Maliev.QuotationService.Api.DTOs.Common;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for creating a new quotation.
/// </summary>
public class CreateQuotationRequest
{
    /// <summary>
    /// The unique identifier of the customer this quotation is for.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Billing identity type (Personal or Corporate).
    /// Determines whether to use customer's Thai National ID or company's tax ID.
    /// </summary>
    [Required]
    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;

    /// <summary>
    /// The unique identifier of the source RFQ, if any.
    /// </summary>
    public Guid? SourceRfqId { get; set; }

    /// <summary>
    /// The start date of the quotation validity period.
    /// </summary>
    public DateTime ValidityPeriodStart { get; set; }

    /// <summary>
    /// The end date of the quotation validity period.
    /// </summary>
    public DateTime ValidityPeriodEnd { get; set; }

    /// <summary>
    /// The line items included in the quotation.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<QuotationLineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// The discount structure to apply to the quotation, if any.
    /// </summary>
    public DiscountStructureDto? DiscountStructure { get; set; }

    /// <summary>
    /// Delivery expectations or notes.
    /// </summary>
    public string? DeliveryExpectations { get; set; }

    /// <summary>
    /// Manual discount amount entered for the quotation.
    /// </summary>
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>
    /// Shipping or delivery cost applied to the quotation.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// VAT or tax amount calculated for the quotation.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Customer-facing special terms shown on generated PDFs.
    /// </summary>
    public string? SpecialTerms { get; set; }
}
