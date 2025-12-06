using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class QuotationLineItemDto
{
    [Required]
    public Guid MaterialServiceId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Required]
    [StringLength(50)]
    public string UnitOfMeasure { get; set; } = string.Empty;
    [Range(0, (double)decimal.MaxValue)]
    public decimal UnitPrice { get; set; }
    [StringLength(200)]
    public string? ManufacturingProcess { get; set; }
    [StringLength(1000)]
    public string? Notes { get; set; }
}
