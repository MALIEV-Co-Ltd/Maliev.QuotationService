using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Data.Entities;

public class DiscountStructure
{
    public Guid Id { get; set; }
    public Guid QuotationVersionId { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public string? Conditions { get; set; }
    public string? AuthorizationReason { get; set; }

    // Navigation properties
    public QuotationVersion QuotationVersion { get; set; } = null!;
}
