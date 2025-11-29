using System.Text.Json;

namespace Maliev.QuotationService.Data.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument? ContactInfo { get; set; }
    public List<Guid>? MergedFromIds { get; set; }
    public JsonDocument? MergeHistory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Rfq> Rfqs { get; set; } = new List<Rfq>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
