using Maliev.QuotationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class CreateRfqRequest
{
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;
    [Phone]
    public string? CustomerPhoneNumber { get; set; }
    public RfqChannel ChannelSource { get; set; }
    public object RequestDetails { get; set; } = new();
    public List<Guid>? UploadServiceFileIds { get; set; }
}
