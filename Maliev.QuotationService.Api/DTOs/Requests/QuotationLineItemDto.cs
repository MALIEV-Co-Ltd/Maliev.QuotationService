using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for a line item in a quotation.
/// </summary>
public class QuotationLineItemDto
{
    /// <summary>
    /// The unique identifier of the material from the Material Service.
    /// </summary>
    [Required]
    public Guid MaterialServiceId { get; set; }

    /// <summary>
    /// The quantity of the line item.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// The unit of measure for the quantity.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// The unit price for this line item.
    /// </summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// The manufacturing process to use.
    /// </summary>
    [StringLength(200)]
    public string? ManufacturingProcess { get; set; }

    /// <summary>
    /// Additional notes for this line item.
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
