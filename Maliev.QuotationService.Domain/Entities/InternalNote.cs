namespace Maliev.QuotationService.Domain.Entities;

public class InternalNote
{
    public Guid Id { get; set; }
    public Guid? RfqId { get; set; }
    public Guid? QuotationId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Rfq? Rfq { get; set; }
    public Quotation? Quotation { get; set; }
}
