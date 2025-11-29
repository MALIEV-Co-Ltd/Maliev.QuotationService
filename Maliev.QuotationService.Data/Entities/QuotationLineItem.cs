using System.Text.Json;

namespace Maliev.QuotationService.Data.Entities;

public class QuotationLineItem
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public int LineNumber { get; set; }
    public Guid MaterialServiceId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public JsonDocument? MaterialProperties { get; set; }
    public string? ManufacturingProcess { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "pcs";
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public QuotationVersion Version { get; set; } = null!;
}
