using System.ComponentModel.DataAnnotations;
using Maliev.QuotationService.Api.DTOs.Common;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class CreateQuotationRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Billing identity type (Personal or Corporate).
    /// Determines whether to use customer's Thai National ID or company's tax ID.
    /// </summary>
    [Required]
    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;

    public Guid? SourceRfqId { get; set; }
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    [Required]
    [MinLength(1)]
    public List<QuotationLineItemDto> LineItems { get; set; } = new();
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
}
