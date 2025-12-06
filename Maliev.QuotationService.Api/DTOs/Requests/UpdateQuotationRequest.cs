using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class UpdateQuotationRequest
{
    public List<QuotationLineItemDto>? LineItems { get; set; }
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
    [Required]
    [StringLength(500)]
    public string ChangeSummary { get; set; } = string.Empty;
}
