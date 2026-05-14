using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for attaching a generated PDF artifact to a specific quotation version.
/// </summary>
public class AttachQuotationVersionPdfRequest
{
    /// <summary>
    /// Customer-facing URL for the generated PDF artifact.
    /// </summary>
    [StringLength(2048)]
    public string? PdfArtifactUrl { get; set; }

    /// <summary>
    /// Internal storage path for the generated PDF artifact.
    /// </summary>
    [StringLength(1024)]
    public string? PdfArtifactStoragePath { get; set; }

    /// <summary>
    /// Timestamp when the PDF artifact was generated.
    /// </summary>
    public DateTime? PdfGeneratedAt { get; set; }
}
