namespace Maliev.QuotationService.Api.DTOs.Requests;

public class UpdateQuotationRequest
{
    public List<QuotationLineItemDto>? LineItems { get; set; }
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
}
