using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Response model for a quotation version.
/// </summary>
public class QuotationVersionResponse
{
    /// <summary>
    /// The unique identifier of the version.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// The line items in this version.
    /// </summary>
    public List<QuotationLineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// The total price for this version.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Manual discount amount applied to this version.
    /// </summary>
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>
    /// Shipping or delivery cost applied to this version.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// VAT or tax amount applied to this version.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// The currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "THB";

    /// <summary>
    /// The discount structure applied to this version.
    /// </summary>
    public DiscountStructureDto? DiscountStructure { get; set; }

    /// <summary>
    /// Delivery expectations for this version.
    /// </summary>
    public string? DeliveryExpectations { get; set; }

    /// <summary>
    /// A summary of changes in this version.
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Customer-facing special terms shown on generated PDFs.
    /// </summary>
    public string? SpecialTerms { get; set; }

    /// <summary>
    /// The user ID who created this version.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when this version was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
