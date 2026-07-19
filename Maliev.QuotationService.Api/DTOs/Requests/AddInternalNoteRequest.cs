using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for adding an internal note to an RFQ or quotation.
/// </summary>
public class AddInternalNoteRequest
{
    /// <summary>
    /// The content of the internal note.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
