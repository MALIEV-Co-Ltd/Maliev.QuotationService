using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? SourceRfqId { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public QuotationStatus Status { get; set; }

    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;
    public DateOnly ValidityPeriodStart { get; set; }
    public DateOnly ValidityPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Rfq? SourceRfq { get; set; }
    public QuotationVersion? CurrentVersion { get; set; }
    public ICollection<QuotationVersion> Versions { get; set; } = new List<QuotationVersion>();
    public ICollection<InternalNote> InternalNotes { get; set; } = new List<InternalNote>();
    public ICollection<FileReference> FileReferences { get; set; } = new List<FileReference>();
}
