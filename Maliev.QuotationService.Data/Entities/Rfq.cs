using System.Text.Json;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Data.Entities;

public class Rfq
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public RfqChannel ChannelSource { get; set; }
    public RfqStatus Status { get; set; }
    public JsonDocument? RequestDetails { get; set; }
    public string? AssignedStaffUserId { get; set; }
    public Guid? ConvertedToQuotationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Quotation? ConvertedToQuotation { get; set; }
    public ICollection<InternalNote> InternalNotes { get; set; } = new List<InternalNote>();
    public ICollection<FileReference> FileReferences { get; set; } = new List<FileReference>();
}
