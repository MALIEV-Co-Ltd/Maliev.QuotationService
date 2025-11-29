namespace Maliev.QuotationService.Data.Entities;

public class FileReference
{
    public Guid Id { get; set; }
    public Guid? RfqId { get; set; }
    public Guid? QuotationId { get; set; }
    public Guid UploadServiceFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;

    // Navigation properties
    public Rfq? Rfq { get; set; }
    public Quotation? Quotation { get; set; }
}
