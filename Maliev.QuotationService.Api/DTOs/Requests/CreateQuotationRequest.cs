using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class CreateQuotationRequest
{
    [Required]
    public Guid CustomerId { get; set; }
    public Guid? SourceRfqId { get; set; }
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    [Required]
    [MinLength(1)]
    public List<QuotationLineItemDto> LineItems { get; set; } = new();
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
}
