using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.DTOs.Responses;

public class QuotationVersionResponse
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public List<QuotationLineItemDto> LineItems { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = "THB";
    public DiscountStructureDto? DiscountStructure { get; set; }
    public string? DeliveryExpectations { get; set; }
    public string? ChangeSummary { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
