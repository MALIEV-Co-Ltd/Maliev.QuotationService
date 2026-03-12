namespace Maliev.QuotationService.Domain.Entities;

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

    public double? VolumeCm3 { get; set; }
    public double? SupportVolumeCm3 { get; set; }
    public double? SurfaceAreaCm2 { get; set; }
    public bool? IsManifold { get; set; }
    public int? TriangleCount { get; set; }

    public Rfq? Rfq { get; set; }
    public Quotation? Quotation { get; set; }
}
