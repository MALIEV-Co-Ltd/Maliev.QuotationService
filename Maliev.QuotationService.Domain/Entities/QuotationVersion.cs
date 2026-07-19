using System.Text.Json;

namespace Maliev.QuotationService.Domain.Entities;

public class QuotationVersion
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public int VersionNumber { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ChangeSummary { get; set; }
    public string? ProjectSnapshotJson { get; set; }
    public string? ProjectSnapshotHash { get; set; }
    public string? PdfArtifactUrl { get; set; }
    public string? PdfArtifactStoragePath { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }
    public string? GeneratedByDisplayName { get; set; }
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public JsonDocument? DeliveryExpectations { get; set; }
    public decimal ManualDiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public string? SpecialTerms { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public ICollection<QuotationLineItem> LineItems { get; set; } = new List<QuotationLineItem>();
    public ICollection<DiscountStructure> DiscountStructures { get; set; } = new List<DiscountStructure>();
}
