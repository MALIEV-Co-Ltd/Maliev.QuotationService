namespace Maliev.QuotationService.Api.DTOs.Requests;

public class QuotationLineItemDto
{
    public Guid MaterialServiceId { get; set; }
    public int Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ManufacturingProcess { get; set; }
    public string? Notes { get; set; }
}
