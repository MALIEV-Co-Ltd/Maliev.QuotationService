using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for updating an existing quotation.
/// </summary>
public class UpdateQuotationRequest
{
    /// <summary>
    /// The line items for the quotation. If null, existing line items are retained.
    /// </summary>
    public List<QuotationLineItemDto>? LineItems { get; set; }

    /// <summary>
    /// The discount structure to apply. If null, existing discount is retained.
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

    /// <summary>
    /// A summary of the changes made in this update.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ChangeSummary { get; set; } = string.Empty;
}
