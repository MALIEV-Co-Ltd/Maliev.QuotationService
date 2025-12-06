using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class AddInternalNoteRequest
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
