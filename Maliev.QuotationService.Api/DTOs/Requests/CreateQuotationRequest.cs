namespace Maliev.QuotationService.Api.DTOs.Requests;

public class CreateQuotationRequest
{
    public Guid CustomerId { get; set; }
    public Guid? SourceRfqId { get; set; }
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    public List<QuotationLineItemDto> LineItems { get; set; } = new();
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
}
