using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Data.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? SourceRfqId { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public QuotationStatus Status { get; set; }

    /// <summary>
    /// Billing identity type (Personal or Corporate) selected for this quotation.
    /// Determines whether customer's Thai National ID or company tax ID is used on documents.
    /// </summary>
    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;
    public DateOnly ValidityPeriodStart { get; set; }
    public DateOnly ValidityPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Rfq? SourceRfq { get; set; }
    public QuotationVersion? CurrentVersion { get; set; }
    public ICollection<QuotationVersion> Versions { get; set; } = new List<QuotationVersion>();
    public ICollection<InternalNote> InternalNotes { get; set; } = new List<InternalNote>();
    public ICollection<FileReference> FileReferences { get; set; } = new List<FileReference>();
}
